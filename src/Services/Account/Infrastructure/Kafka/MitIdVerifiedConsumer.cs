using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Contracts.Messaging;
using BuildingBlocks.Contracts.Events;
using Domain.Repositories;

namespace Infrastructure.Kafka
{
    public class MitIdVerifiedConsumer : BackgroundService
    {
        private readonly IKafkaConsumer _consumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MitIdVerifiedConsumer> _logger;

        public MitIdVerifiedConsumer(
            IKafkaConsumer consumer,
            IServiceScopeFactory scopeFactory,
            ILogger<MitIdVerifiedConsumer> logger)
        {
            _consumer = consumer;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _consumer.ConsumeAsync<MitIdVerified>(
                Topics.MitIdVerified,
                HandleMessageAsync,
                stoppingToken);
        }

        private async Task HandleMessageAsync(MitIdVerified message, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

            var account = await repo.GetByIdAsync(message.AccountId, ct);
            if (account == null)
            {
                _logger.LogWarning("Account {AccountId} not found for MitIdVerified event", message.AccountId);
                return;
            }

            account.ApplyMitIdVerification(message.MitIdSubId, message.IsAdult);
            await repo.UpdateAsync(account, ct);

            _logger.LogInformation("Updated Account {AccountId} with MitId verification", message.AccountId);
        }
    }
}
