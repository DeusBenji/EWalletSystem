using System.Security.Cryptography;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Contracts.Messaging;
using System.Text.Json;

namespace BuildingBlocks.Kafka
{
    public class KafkaConsumer : IKafkaConsumer
    {
        private readonly IConfiguration _config;
        private readonly ILogger<KafkaConsumer> _logger;
        private readonly IKafkaProducer _producer;

        // DLQ Config
        private readonly bool _dlqEnabled;
        private readonly string _dlqTopicSuffix;
        private readonly int _maxAttempts;
        private readonly int _backoffBaseMs;
        private readonly int _backoffMaxMs;
        private readonly double _jitterPct;
        private readonly bool _onPublishFailureCrash; 
        private readonly bool _includeStackTrace;

        public KafkaConsumer(IConfiguration config, ILogger<KafkaConsumer> logger, IKafkaProducer producer)
        {
            _config = config;
            _logger = logger;
            _producer = producer;

            // Load config
            _dlqEnabled = _config.GetValue("Kafka:DLQ:Enabled", true);
            _dlqTopicSuffix = _config.GetValue("Kafka:DLQ:TopicSuffix", ".DLQ");
            _maxAttempts = _config.GetValue("Kafka:DLQ:MaxAttempts", 5);
            _backoffBaseMs = _config.GetValue("Kafka:DLQ:BackoffBaseMs", 200);
            _backoffMaxMs = _config.GetValue("Kafka:DLQ:BackoffMaxMs", 5000);
            _jitterPct = _config.GetValue("Kafka:DLQ:JitterPct", 0.2);
            // Enforce Crash on DLQ failure to prevent data loss.
            _includeStackTrace = _config.GetValue("Kafka:DLQ:IncludeStackTrace", true);
        }

        public async Task ConsumeAsync<T>(string topic, Func<T, CancellationToken, Task> handler, CancellationToken ct = default)
        {
            var bootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092";
            var groupId = _config["Kafka:GroupId"] ?? "default-group";

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false 
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(topic);
            _logger.LogInformation("Subscribed to {Topic} with GroupId {GroupId} (DLQ={DlqEnabled})", topic, groupId, _dlqEnabled);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(ct);
                        if (consumeResult == null) continue;

                        // Delegate processing to internal method (testable)
                        var result = await ProcessMessageInternal(consumeResult, handler, ct, groupId);

                        if (result.ShouldCommit)
                        {
                            consumer.Commit(consumeResult);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error consuming from {Topic}", topic);
                        await Task.Delay(1000, ct);
                    }
                    catch (Exception ex)
                    {
                        // Ensure fatal errors bubble up (or crash policy) if needed, but for consume loop we usually try to survive.
                        // However, ProcessMessageInternal handles the per-message crash policy.
                        _logger.LogError(ex, "Unexpected error in consume loop for {Topic}", topic);
                        await Task.Delay(5000, ct); 
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consuming cancelled for {Topic}", topic);
            }
            finally
            {
                consumer.Close();
            }
        }

        // Internal struct to control commit behavior
        public struct ProcessResult
        {
            public bool ShouldCommit { get; set; }
            public bool Success { get; set; }
        }

        // Internal for testing (or protected/internal)
        public async Task<ProcessResult> ProcessMessageInternal<T>(ConsumeResult<string, string> consumeResult, Func<T, CancellationToken, Task> handler, CancellationToken ct, string consumerGroup = "unknown")
        {
            var topic = consumeResult.Topic;
            T? message = default;
            string rawPayloadString = consumeResult.Message.Value;

            // 1. DESERIALIZE
            try
            {
                message = JsonSerializer.Deserialize<T>(rawPayloadString);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Deserialization failed for {Topic} [Part:{Partition} Off:{Offset}]. Sending to DLQ.", topic, consumeResult.Partition, consumeResult.Offset);
                
                // Publish to DLQ as invalid format
                await PublishToDlq(consumeResult, rawPayloadString, ex, "DeserializationException", consumerGroup, ct);
                
                // Commit to skip this bad message
                return new ProcessResult { ShouldCommit = true, Success = false };
            }

            if (message == null)
            {
                // Null payload? Commit and skip.
                 return new ProcessResult { ShouldCommit = true, Success = true };
            }

            // 2. RETRY LOOP
            int attempt = 0;
            while (attempt < _maxAttempts)
            {
                attempt++;
                try
                {
                    // Execute Handler
                    await handler(message, ct);
                    
                    // Success!
                    return new ProcessResult { ShouldCommit = true, Success = true };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Processing failed for {Topic} [Part:{Partition} Off:{Offset}] Attempt {Attempt}/{Max}", 
                        topic, consumeResult.Partition, consumeResult.Offset, attempt, _maxAttempts);

                    if (attempt < _maxAttempts)
                    {
                        // Backoff
                        await Task.Delay(CalculateBackoff(attempt), ct);
                    }
                    else
                    {
                        // Max attempts reached. Send to DLQ.
                        _logger.LogError("Max attempts reached for {Topic} [Part:{Partition} Off:{Offset}]. Sending to DLQ.", topic, consumeResult.Partition, consumeResult.Offset);
                        
                        await PublishToDlq(consumeResult, rawPayloadString, ex, ex.GetType().Name, consumerGroup, ct);
                        
                        // If PublishToDlq returns (didn't crash), we commit.
                        return new ProcessResult { ShouldCommit = true, Success = false };
                    }
                }
            }

            return new ProcessResult { ShouldCommit = false, Success = false }; // Should be unreachable given loop logic
        }

        private async Task PublishToDlq(ConsumeResult<string, string> original, string rawPayload, Exception ex, string errorType, string consumerGroup, CancellationToken ct)
        {
            if (!_dlqEnabled) 
            {
                _logger.LogWarning("DLQ disabled. Message discarded.");
                return;
            }

            var dlqTopic = original.Topic + _dlqTopicSuffix;
            var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawPayload ?? ""));
            var keyBase64 = original.Message.Key != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(original.Message.Key)) : null;

            var dlqId = ComputeHash($"{original.Topic}-{original.Partition.Value}-{original.Offset.Value}");

            var envelope = new DlqEnvelope
            {
                SchemaVersion = 1,
                OriginalTopic = original.Topic,
                OriginalPartition = original.Partition.Value,
                OriginalOffset = original.Offset.Value,
                ConsumerGroup = consumerGroup,
                OriginalKeyBase64 = keyBase64,
                OriginalHeaders = SanitizeHeaders(original.Message.Headers),
                OriginalPayloadBase64 = payloadBase64,
                Error = ex.Message,
                ErrorType = errorType,
                StackTrace = _includeStackTrace ? Truncate(ex.StackTrace, 4096) : null,
                FailedAtUtc = DateTimeOffset.UtcNow,
                AttemptCount = _maxAttempts,
                DlqMessageId = dlqId
            };

            try
            {
                // Use dlqId as Kafka Key for data locality/idempotency
                await _producer.PublishAsync(dlqTopic, dlqId, envelope, ct);
                
                _logger.LogInformation("Published to DLQ {DlqTopic} [ID:{DlqId}]", dlqTopic, dlqId);
            }
            catch (Exception dlqEx)
            {
                _logger.LogCritical(dlqEx, "FATAL: Failed to publish to DLQ {DlqTopic} for {OriginalTopic} [Part:{Part} Off:{Off}]. CRASHING to prevent data loss.", 
                    dlqTopic, original.Topic, original.Partition, original.Offset);
                
                // ALWAYS Crash/Throw to trigger restart/rebalance. Never commit if DLQ fails.
                throw; 
            }
        }

        private int CalculateBackoff(int attempt)
        {
            // Exponential: base * 2^(attempt-1)
            double delay = _backoffBaseMs * Math.Pow(2, attempt - 1);
            delay = Math.Min(delay, _backoffMaxMs);
            
            // Jitter: +/- JitterPct
            var random = Random.Shared; 
            var jitter = delay * _jitterPct * (random.NextDouble() * 2 - 1); // -1 to 1
            
            return (int)Math.Max(0, delay + jitter);
        }

        private Dictionary<string, string> SanitizeHeaders(Headers headers)
        {
            if (headers == null) return new Dictionary<string, string>();
            
            var sensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
            { 
                "authorization", "token", "secret", "cookie", "password", "apikey",
                "set-cookie", "x-api-key", "session"
            };

            var dict = new Dictionary<string, string>();
            foreach (var h in headers)
            {
                if (sensitiveKeys.Contains(h.Key))
                {
                    dict[h.Key] = "[REDACTED]";
                }
                else
                {
                    try { dict[h.Key] = Encoding.UTF8.GetString(h.GetValueBytes()); }
                    catch { dict[h.Key] = "[BINARY]"; }
                }
            }
            return dict;
        }

        private string ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        private string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }
    }
}

