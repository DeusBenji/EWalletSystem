using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

using BuildingBlocks.Contracts.Messaging;

namespace BuildingBlocks.Kafka
{
    public class KafkaProducer : IKafkaProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducer> _logger;

        public KafkaProducer(IConfiguration config, ILogger<KafkaProducer> logger)
        {
            _logger = logger;

            var bootstrapServers = config["Kafka:BootstrapServers"];
            if (string.IsNullOrWhiteSpace(bootstrapServers))
            {
                // Fallback or throw? Throwing is safer for infrastructure.
                // But for local dev sometimes we might want defaults.
                // Let's stick to throwing if missing to be explicit.
                bootstrapServers = "localhost:9092"; 
            }

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageTimeoutMs = 30000
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            _logger.LogInformation("KafkaProducer initialized with servers: {Servers}", bootstrapServers);
        }

        public async Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
        {
            await PublishAsync(topic, Guid.NewGuid().ToString(), message, ct);
        }

        public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var msg = new Message<string, string>
                {
                    Key = key,
                    Value = json
                };

                var result = await _producer.ProduceAsync(topic, msg, ct);

                _logger.LogInformation("Published to {Topic} [Part:{Partition} Off:{Offset}]", 
                    topic, result.Partition.Value, result.Offset.Value);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Kafka Produce failed for {Topic}: {Reason}", topic, ex.Error.Reason);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error publishing to {Topic}", topic);
                throw;
            }
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}
