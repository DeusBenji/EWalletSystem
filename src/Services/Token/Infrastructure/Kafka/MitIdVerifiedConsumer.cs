using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Contracts.Messaging;
using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Kafka
{
    public class MitIdVerifiedConsumer : BackgroundService
    {
        private readonly ILogger<MitIdVerifiedConsumer> _logger;
        private readonly IAccountAgeStatusRepository _accountAgeStatusRepository;
        private readonly IConsumer<string, string> _consumer;
        private readonly string _topic;

        public MitIdVerifiedConsumer(
            IConfiguration configuration,
            ILogger<MitIdVerifiedConsumer> logger,
            IAccountAgeStatusRepository accountAgeStatusRepository)
        {
            _logger = logger;
            _accountAgeStatusRepository = accountAgeStatusRepository;

            var bootstrapServers = configuration["Kafka:BootstrapServers"];
            if (string.IsNullOrWhiteSpace(bootstrapServers))
            {
                throw new InvalidOperationException("Missing Kafka configuration 'Kafka:BootstrapServers'.");
            }

            // Du kan evt. have en specifik GroupId til TokenService-consumer
            var groupId = configuration["Kafka:GroupId"] ?? "token-service";

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
            _topic = Topics.MitIdVerified;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MitIdVerifiedConsumer starting, subscribing to topic {Topic}", _topic);

            _consumer.Subscribe(_topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var cr = _consumer.Consume(stoppingToken);
                        if (cr is null || cr.Message is null)
                            continue;

                        _logger.LogInformation("Received MitIdVerified message at {TopicPartitionOffset}", cr.TopicPartitionOffset);

                        MitIdVerified? evt;
                        try
                        {
                            evt = JsonSerializer.Deserialize<MitIdVerified>(cr.Message.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to deserialize MitIdVerified message: {Payload}", cr.Message.Value);
                            continue;
                        }

                        if (evt is null)
                        {
                            _logger.LogWarning("Deserialized MitIdVerified event was null");
                            continue;
                        }

                        var status = new AccountAgeStatus(
                            evt.AccountId,
                            evt.IsAdult,
                            evt.VerifiedAt);

                        await _accountAgeStatusRepository.SaveAsync(status, stoppingToken);

                        _logger.LogInformation(
                            "Updated AccountAgeStatus for AccountId {AccountId}, IsAdult={IsAdult}",
                            evt.AccountId,
                            evt.IsAdult);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                        // eventuelt continue; og lad loopet fortsætte
                    }
                    catch (OperationCanceledException)
                    {
                        // normal ved shutdown
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error in MitIdVerifiedConsumer loop");
                    }
                }
            }
            finally
            {
                _logger.LogInformation("MitIdVerifiedConsumer is closing Kafka consumer");
                _consumer.Close();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _consumer.Dispose();
        }
    }
}
