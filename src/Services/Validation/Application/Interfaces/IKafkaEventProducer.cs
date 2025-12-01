using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IKafkaEventProducer
    {
        Task PublishAsync(string topic, string key, string value);
        Task PublishAsync<TEvent>(string topic, string key, TEvent @event)
            where TEvent : class;
    }
}
