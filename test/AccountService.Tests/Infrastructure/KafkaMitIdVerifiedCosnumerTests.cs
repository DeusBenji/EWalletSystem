using System;
using System.Threading;
using System.Threading.Tasks;
using AccountService.API.BackgroundServices;
using Application.Interfaces;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AccountSerrvice.Test.Infrastructure
{
    internal class TestableMitIdVerifiedConsumer : MitIdVerifiedConsumer
    {
        public TestableMitIdVerifiedConsumer(
            IKafkaConsumer kafkaConsumer,
            IServiceScopeFactory scopeFactory,
            ILogger<MitIdVerifiedConsumer> logger)
            : base(kafkaConsumer, scopeFactory, logger)
        {
        }

        public Task InvokeExecuteAsync(CancellationToken token) => ExecuteAsync(token);
    }

    public class KafkaMitIdVerifiedCosnumerTests
    {
        [Fact]
        public async Task ExecuteAsync_WhenMitIdVerifiedMessageConsumed_CallsMitIdVerifiedService()
        {
            // Arrange
            var kafkaConsumerMock = new Mock<IKafkaConsumer>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var mitIdVerifiedServiceMock = new Mock<IMitIdVerifiedService>();
            var loggerMock = new Mock<ILogger<MitIdVerifiedConsumer>>();

            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.SetupGet(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IMitIdVerifiedService)))
                .Returns(mitIdVerifiedServiceMock.Object);

            var accountId = Guid.NewGuid();
            var isAdult = true;

            kafkaConsumerMock
                .Setup(k => k.ConsumeAsync<MitIdVerified>(
                    It.IsAny<string>(),
                    It.IsAny<Func<MitIdVerified, CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, Func<MitIdVerified, CancellationToken, Task>, CancellationToken>(
                    (topic, handler, ct) =>
                    {
                        var message = new MitIdVerified(
                            accountId,
                            isAdult,
                            DateTime.UtcNow
                        );

                        return handler(message, ct);
                    });

            var sut = new TestableMitIdVerifiedConsumer(
                kafkaConsumerMock.Object,
                scopeFactoryMock.Object,
                loggerMock.Object);

            // Act
            await sut.InvokeExecuteAsync(CancellationToken.None);

            // Assert
            mitIdVerifiedServiceMock.Verify(
                s => s.HandleMitIdVerifiedAsync(accountId, isAdult),
                Times.Once);

            kafkaConsumerMock.Verify(
                k => k.ConsumeAsync<MitIdVerified>(
                    Topics.MitIdVerified,
                    It.IsAny<Func<MitIdVerified, CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
