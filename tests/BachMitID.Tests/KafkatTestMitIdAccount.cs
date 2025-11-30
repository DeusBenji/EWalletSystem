using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BachMitID.Application.Contracts;
using BachMitID.Infrastructure.Kafka;
using BachMitID.Infrastructure.Kafka.Interfaces; // hvis du bruger dette namespace til interfacet
using Confluent.Kafka;
using Moq;
using Xunit;

public class KafkaTestMitIdAccount
{
    [Fact]
    public async Task PublishCreatedAsync_SendsMessage_WithCorrectTopicKeyAndJson()
    {
        // arrange
        var mockProducer = new Mock<IProducer<string, string>>();

        string? capturedTopic = null;
        Message<string, string>? capturedMessage = null;

        // Når publisher kalder ProduceAsync, fanger vi topic + message
        mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((topic, message, token) =>
            {
                capturedTopic = topic;
                capturedMessage = message;
            })
            .ReturnsAsync(new DeliveryResult<string, string>());

        var topic = "test.mitid.account.created";
        var publisher = new MitIdAccountEventPublisher(mockProducer.Object, topic);

        var evt = new MitIdAccountCreatedEvent
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            SubId = "hashed-sub-123",
            IsAdult = true
        };

        // act
        await publisher.PublishCreatedAsync(evt);

        // assert
        // 1) Topic skal være korrekt
        Assert.Equal(topic, capturedTopic);

        // 2) Message må ikke være null
        Assert.NotNull(capturedMessage);
        Assert.Equal(evt.AccountId.ToString(), capturedMessage!.Key);

        // 3) JSON-indhold skal svare til eventet
        var deserialized = JsonSerializer.Deserialize<MitIdAccountCreatedEvent>(capturedMessage.Value);
        Assert.NotNull(deserialized);
        Assert.Equal(evt.Id, deserialized!.Id);
        Assert.Equal(evt.AccountId, deserialized.AccountId);
        Assert.Equal(evt.SubId, deserialized.SubId);
        Assert.Equal(evt.IsAdult, deserialized.IsAdult);

        // 4) Producer skal være kaldt præcis én gang
        mockProducer.Verify(p => p.ProduceAsync(
                topic,
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishCreatedAsync_CallsProducerOnce_EvenForSameEvent()
    {
        // arrange
        var mockProducer = new Mock<IProducer<string, string>>();

        mockProducer
            .Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>());

        var topic = "another.test.topic";
        var publisher = new MitIdAccountEventPublisher(mockProducer.Object, topic);

        var evt = new MitIdAccountCreatedEvent
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            SubId = "hashed-sub-999",
            IsAdult = false
        };

        // act
        await publisher.PublishCreatedAsync(evt);

        // assert
        mockProducer.Verify(p => p.ProduceAsync(
                topic,
                It.Is<Message<string, string>>(m => m.Key == evt.AccountId.ToString()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
