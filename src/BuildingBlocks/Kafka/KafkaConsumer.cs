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

        public KafkaConsumer(IConfiguration config, ILogger<KafkaConsumer> logger)
        {
            _config = config;
            _logger = logger;
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
            _logger.LogInformation("Subscribed to {Topic} with GroupId {GroupId}", topic, groupId);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(ct);
                        if (consumeResult == null) continue;

                        _logger.LogDebug("Received message on {Topic}", topic);

                        T? message = default;
                        try 
                        {
                             message = JsonSerializer.Deserialize<T>(consumeResult.Message.Value);
                        }
                        catch(JsonException ex)
                        {
                             _logger.LogError(ex, "Failed to deserialize message from {Topic}", topic);
                             // Commit anyway to skip bad message
                             consumer.Commit(consumeResult);
                             continue;
                        }

                        if (message != null)
                        {
                            await handler(message, ct);
                        }

                        consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error consuming from {Topic}", topic);
                        // Wait briefly to avoid tight loop on persistent errors
                        await Task.Delay(1000, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error consuming from {Topic}", topic);
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
    }
}
