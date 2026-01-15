using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Kafka;
using BuildingBlocks.Contracts.Messaging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

public class KafkaProducerTests
{
    private class TestPublisher
    {
        private readonly IKafkaProducer _producer;

        public TestPublisher(IKafkaProducer producer)
        {
            _producer = producer;
        }

        public Task PublishAsync<T>(T message, string topic, string key, CancellationToken ct = default)
        {
            return _producer.PublishAsync(topic, key, message!, ct);
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

        var topic = "user-created";
        var key = Guid.NewGuid().ToString();

        var message = new
        {
            Id = Guid.NewGuid(),
            Email = "user@test.dk"
        };

        // act
        await publisher.PublishAsync(message, topic, key);

        // assert
        Assert.Equal(topic, capturedTopic);
        Assert.Equal(key, capturedKey);

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(capturedJson!);

        Assert.NotNull(deserialized);
        Assert.Equal(message.Email, deserialized!["Email"]!.ToString());

        mockKafka.Verify(p => p.PublishAsync(
                topic,
                key,
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

        var topic = "user-created";
        var key = "user-123";

        var message = new
        {
            Id = Guid.NewGuid(),
            Email = "abc@test.dk"
        };

        // act
        await publisher.PublishAsync(message, topic, key);

        // assert
        mockKafka.Verify(p => p.PublishAsync(
                topic,
                key,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
