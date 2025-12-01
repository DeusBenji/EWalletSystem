using Application.Interfaces;
using Shared.Infrastructure.Kafka;

namespace Infrastructure.Kafka
{
    public class KafkaEventProducer : IKafkaEventProducer
    {
        private readonly Shared.Infrastructure.Kafka.IKafkaProducer _producer;

        public KafkaEventProducer(Shared.Infrastructure.Kafka.IKafkaProducer producer)
        {
            _producer = producer;
        }

        public Task PublishAsync<TEvent>(string topic, string key, TEvent @event) where TEvent : class
        {
            return _producer.PublishAsync(topic, key, @event);
        }

        public Task PublishAsync(string topic, string key, string value)
        {
            return _producer.PublishAsync(topic, key, value);
        }
    }
}
