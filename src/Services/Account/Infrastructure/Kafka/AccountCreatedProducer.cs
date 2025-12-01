using Application.Interfaces;
using BuildingBlocks.Contracts.Messaging;

namespace Infrastructure.Kafka
{
    public sealed class AccountCreatedProducer : Application.Interfaces.IKafkaProducer
    {
        private readonly BuildingBlocks.Contracts.Messaging.IKafkaProducer _producer;

        public AccountCreatedProducer(BuildingBlocks.Contracts.Messaging.IKafkaProducer producer)
        {
            _producer = producer;
        }

        public Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
        {
            return _producer.PublishAsync(topic, message, ct);
        }
    }
}
