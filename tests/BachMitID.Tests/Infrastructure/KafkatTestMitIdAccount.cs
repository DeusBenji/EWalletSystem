using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;
using Moq;
using Xunit;

public class MitIdAccountPublisherTests
{
    private class TestPublisher
    {
        private readonly IKafkaProducer _producer;

        public TestPublisher(IKafkaProducer producer)
        {
            _producer = producer;
        }

        public Task PublishAsync(MitIdAccountCreated evt, string topic, CancellationToken ct = default)
        {
            return _producer.PublishAsync(topic, evt.AccountId.ToString(), evt, ct);
        }
    }

    [Fact]
    public async Task PublishAsync_SendsMessage_WithCorrectTopicKeyAndJson()
    {
        // arrange
        var mockKafka = new Mock<IKafkaProducer>();

        string? capturedTopic = null;
        string? capturedKey = null;
        string? capturedJson = null;

        mockKafka
            .Setup(p => p.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, object, CancellationToken>((topic, key, message, ct) =>
            {
                capturedTopic = topic;
                capturedKey = key;
                capturedJson = JsonSerializer.Serialize(message);
            })
            .Returns(Task.CompletedTask);

        var publisher = new TestPublisher(mockKafka.Object);
        var topicName = "mitid-account-created";

        var evt = new MitIdAccountCreated(
            Id: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            SubId: "hashed-sub-123",
            IsAdult: true,
            CreatedAt: DateTime.UtcNow
        );

        // act
        await publisher.PublishAsync(evt, topicName);

        // assert
        Assert.Equal(topicName, capturedTopic);
        Assert.Equal(evt.AccountId.ToString(), capturedKey);

        // deserialize JSON to validate content
        var deserialized = JsonSerializer.Deserialize<MitIdAccountCreated>(capturedJson!);

        Assert.NotNull(deserialized);
        Assert.Equal(evt.Id, deserialized!.Id);
        Assert.Equal(evt.AccountId, deserialized.AccountId);
        Assert.Equal(evt.SubId, deserialized.SubId);
        Assert.Equal(evt.IsAdult, deserialized.IsAdult);

        mockKafka.Verify(p => p.PublishAsync(
                topicName,
                evt.AccountId.ToString(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_CallsProducer_Once()
    {
        // arrange
        var mockKafka = new Mock<IKafkaProducer>();

        mockKafka
            .Setup(p => p.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var publisher = new TestPublisher(mockKafka.Object);
        var topicName = "mitid-account-created";

        var evt = new MitIdAccountCreated(
            Id: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            SubId: "hashed-sub-999",
            IsAdult: false,
            CreatedAt: DateTime.UtcNow
        );

        // act
        await publisher.PublishAsync(evt, topicName);

        // assert
        mockKafka.Verify(p => p.PublishAsync(
                topicName,
                evt.AccountId.ToString(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
