using System;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IMitIdVerifiedService
    {
        Task HandleMitIdVerifiedAsync(Guid accountId, bool isAdult);
    }
}
