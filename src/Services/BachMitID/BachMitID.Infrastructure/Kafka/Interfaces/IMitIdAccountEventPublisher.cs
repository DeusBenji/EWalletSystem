using BachMitID.Application.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BachMitID.Infrastructure.Kafka.Interfaces
{
    public interface IMitIdAccountEventPublisher
    {
        Task PublishCreatedAsync(MitIdAccountCreatedEvent evt);
    }
}
