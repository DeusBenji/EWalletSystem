using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Contracts.Messaging
{
    public interface IKafkaProducer
    {
        Task PublishAsync<T>(string topic, T message, CancellationToken ct = default);
        Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default);
    }
}
