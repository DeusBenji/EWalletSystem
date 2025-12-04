using System.Threading;
using System.Threading.Tasks;
using BachMitID.Application.BusinessLogicLayer.Interface;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BachMitID.BackgroundServices
{
    /// <summary>
    /// Lytter på AccountCreated-events fra Account-servicen
    /// og sørger for at sync'e accounts ind i BachMitID's egen database.
    /// </summary>
    public class AccountCreatedConsumer : BackgroundService
    {
        private readonly IKafkaConsumer _kafkaConsumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AccountCreatedConsumer> _logger;

        public AccountCreatedConsumer(
            IKafkaConsumer kafkaConsumer,
            IServiceScopeFactory scopeFactory,
            ILogger<AccountCreatedConsumer> logger)
        {
            _kafkaConsumer = kafkaConsumer;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "AccountCreatedConsumer starting. Subscribing to topic {Topic}.",
                Topics.AccountCreated
            );

            await _kafkaConsumer.ConsumeAsync<AccountCreated>(
                topic: Topics.AccountCreated,
                handler: async (message, ct) =>
                {
                    _logger.LogInformation(
                        "Received AccountCreated event in BachMitID. AccountId={AccountId}, Email={Email}, CreatedAt={CreatedAt}",
                        message.AccountId,
                        message.Email,
                        message.CreatedAt
                    );

                    // Opret et scope for at bruge scoped services (fx IAccountSyncService)
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var syncService = scope.ServiceProvider.GetRequiredService<IAccountSyncService>();

                        await syncService.SyncAccountAsync(
                            message.AccountId,
                            message.Email
                        );
                    }
                },
                ct: stoppingToken
            );

            _logger.LogInformation("AccountCreatedConsumer stopping.");
        }
    }
}
