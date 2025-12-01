using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Domain.Repositories;

namespace Infrastructure.Kafka
{
    public class MitIdVerifiedConsumer : BackgroundService
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MitIdVerifiedConsumer> _logger;

        private const string Topic = "mitid-verified";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MitIdVerifiedConsumer(
            IConsumer<Ignore, string> consumer,
            IServiceScopeFactory scopeFactory,
            ILogger<MitIdVerifiedConsumer> logger)
        {
            _consumer = consumer;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Kafka consumer for topic {Topic}", Topic);

            _consumer.Subscribe(Topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    ConsumeResult<Ignore, string>? result = null;

                    try
                    {
                        // Blokerer indtil der kommer en besked eller cancellation
                        result = _consumer.Consume(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Normal shutdown
                        break;
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error on topic {Topic}", Topic);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error while consuming from Kafka");
                        continue;
                    }

                    if (result?.Message?.Value == null)
                        continue;

                    MitIdVerified? evt = null;

                    try
                    {
                        evt = JsonSerializer.Deserialize<MitIdVerified>(result.Message.Value, JsonOptions);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize MitIdVerifiedEvent: {Payload}", result.Message.Value);
                        continue;
                    }

                    if (evt is null)
                    {
                        _logger.LogWarning("Deserialized MitIdVerifiedEvent is null. Payload: {Payload}", result.Message.Value);
                        continue;
                    }

                    try
                    {
                        await HandleEventAsync(evt, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while handling MitIdVerifiedEvent for AccountId {AccountId}", evt.AccountId);
                    }
                }
            }
            finally
            {
                _logger.LogInformation("Stopping Kafka consumer for topic {Topic}", Topic);
                _consumer.Close();
            }
        }

        private async Task HandleEventAsync(MitIdVerified evt, CancellationToken ct)
        {
            _logger.LogInformation("Handling MitIdVerifiedEvent for AccountId {AccountId}", evt.AccountId);

            // Opret et scope så vi kan bruge scoped services
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

            var account = await repo.GetByIdAsync(evt.AccountId, ct);
            if (account is null)
            {
                _logger.LogWarning("MitIdVerified for unknown account {AccountId}", evt.AccountId);
                return;
            }

            account.ApplyMitIdVerification(evt.MitIdSubId, evt.IsAdult);
            await repo.UpdateAsync(account, ct);

            _logger.LogInformation("Applied MitID verification for account {AccountId}", evt.AccountId);
        }
    }
}
