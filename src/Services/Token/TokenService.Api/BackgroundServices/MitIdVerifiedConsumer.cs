using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.BackgroundServices
{
    public class MitIdVerifiedConsumer : BackgroundService
    {
        private readonly IKafkaConsumer _kafkaConsumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MitIdVerifiedConsumer> _logger;

        public MitIdVerifiedConsumer(
            IKafkaConsumer kafkaConsumer,
            IServiceScopeFactory scopeFactory,
            ILogger<MitIdVerifiedConsumer> logger)
        {
            _kafkaConsumer = kafkaConsumer;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MitIdVerifiedConsumer starting. Subscribing to {Topic}", Topics.MitIdVerified);

            await _kafkaConsumer.ConsumeAsync<MitIdVerified>(
                topic: Topics.MitIdVerified,
                handler: async (message, ct) =>
                {
                    _logger.LogInformation(
                        "Received MitIdVerified event. AccountId={AccountId}, IsAdult={IsAdult}, VerifiedAt={VerifiedAt}",
                        message.AccountId,
                        message.IsAdult,
                        message.VerifiedAt);

                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IMitIdVerifiedService>();

                    await service.HandleMitIdVerifiedAsync(
                        message.AccountId,
                        message.IsAdult,
                        message.VerifiedAt,
                        ct
                    );
                },
                ct: stoppingToken);

            _logger.LogInformation("MitIdVerifiedConsumer stopping.");
        }
    }
}
