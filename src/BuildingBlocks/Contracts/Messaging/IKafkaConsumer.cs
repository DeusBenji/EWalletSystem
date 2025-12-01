using System;
using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Contracts.Messaging
{
    public interface IKafkaConsumer
    {
        Task ConsumeAsync<T>(string topic, Func<T, CancellationToken, Task> handler, CancellationToken ct = default);
    }
}
