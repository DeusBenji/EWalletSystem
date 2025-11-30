using BachMitID.Application.BusinessLogicLayer;
using BachMitID.Application.BusinessLogicLayer.Interface;
using BachMitID.Application.Contracts;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BachMitID.Infrastructure.Kafka
{
    public class AccountCreatedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<AccountCreatedConsumer> _logger;

        public AccountCreatedConsumer(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<AccountCreatedConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumerCfg = new ConsumerConfig
            {
                BootstrapServers = _config["Kafka:BootstrapServers"],
                GroupId = "mitid-account-consumer",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerCfg).Build();
            consumer.Subscribe(_config["Kafka:Topics:AccountCreated"]);

            _logger.LogInformation("AccountCreatedConsumer is now listening...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    var evt = JsonSerializer.Deserialize<AccountCreatedEvent>(result.Message.Value);
                    if (evt == null)
                    {
                        _logger.LogWarning("Received null AccountCreatedEvent");
                        continue;
                    }

                    _logger.LogInformation($"Received AccountCreatedEvent: {evt.Id}, {evt.Email}");

                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IMitIdAccountService>();

                    // TODO: opdater MitID-account, associer AccountID, etc.
                    // fx:
                    // await service.LinkAccountAsync(evt.Id, evt.Email);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while consuming AccountCreated event");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
