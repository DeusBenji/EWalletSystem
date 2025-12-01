using Application.Interfaces;
using Shared.Infrastructure.Kafka;

namespace Infrastructure.Kafka
{
    public sealed class AccountCreatedProducer : Application.Interfaces.IKafkaProducer
    {
        private readonly Shared.Infrastructure.Kafka.IKafkaProducer _producer;

        public AccountCreatedProducer(Shared.Infrastructure.Kafka.IKafkaProducer producer)
        {
            _producer = producer;
        }

        public Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
        {
            return _producer.PublishAsync(topic, message, ct);
        }
    }
}
