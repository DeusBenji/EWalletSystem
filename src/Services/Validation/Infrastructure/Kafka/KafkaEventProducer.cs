using Application.Interfaces;
using BuildingBlocks.Contracts.Messaging;

namespace Infrastructure.Kafka
{
    public class KafkaEventProducer : IKafkaEventProducer
    {
        private readonly BuildingBlocks.Contracts.Messaging.IKafkaProducer _producer;

        public KafkaEventProducer(BuildingBlocks.Contracts.Messaging.IKafkaProducer producer)
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
