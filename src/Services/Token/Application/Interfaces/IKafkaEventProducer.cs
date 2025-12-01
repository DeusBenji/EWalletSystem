using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IKafkaEventProducer
    {
        Task PublishAsync<T>(string topic, T message, CancellationToken ct = default);
    }
}
