using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Contracts.Messaging;
using BuildingBlocks.Contracts.Events;
using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Kafka
{
    public class MitIdVerifiedConsumer : BackgroundService
    {
        private readonly IKafkaConsumer _consumer;
        private readonly IAccountAgeStatusRepository _accountAgeStatusRepository;
        private readonly ILogger<MitIdVerifiedConsumer> _logger;

        public MitIdVerifiedConsumer(
            IKafkaConsumer consumer,
            IAccountAgeStatusRepository accountAgeStatusRepository,
            ILogger<MitIdVerifiedConsumer> logger)
        {
            _consumer = consumer;
            _accountAgeStatusRepository = accountAgeStatusRepository;
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
             var status = new AccountAgeStatus(
                message.AccountId,
                message.IsAdult,
                message.VerifiedAt);

            await _accountAgeStatusRepository.SaveAsync(status, ct);

            _logger.LogInformation(
                "Updated AccountAgeStatus for AccountId {AccountId}, IsAdult={IsAdult}",
                message.AccountId,
                message.IsAdult);
        }
    }
}
