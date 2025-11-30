using System.Text.Json;
using BachMitID.Application.Contracts;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace BachMitID.Infrastructure.Kafka
{
    // 1) Interface
    public interface IMitIdAccountEventPublisher
    {
        Task PublishCreatedAsync(MitIdAccountCreatedEvent evt);
    }

    // 2) Implementation
    public class MitIdAccountEventPublisher : IMitIdAccountEventPublisher
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _topic;

        public MitIdAccountEventPublisher(IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _topic = configuration["Kafka:Topics:MitIdAccountCreated"] ?? "mitid.account.created";
        }

        // TEST/DI-constructor – ny
        public MitIdAccountEventPublisher(IProducer<string, string> producer, string topic)
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        }


        public async Task PublishCreatedAsync(MitIdAccountCreatedEvent evt)
        {
            var json = JsonSerializer.Serialize(evt);

            var message = new Message<string, string>
            {
                Key = evt.AccountId.ToString(),
                Value = json
            };

            await _producer.ProduceAsync(_topic, message);
        }
    }
}
