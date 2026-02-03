using System.Text;
using System.Text.Json;
using BuildingBlocks.Contracts.Messaging;
using BuildingBlocks.Kafka;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace BuildingBlocks.Tests.Kafka;

public class KafkaConsumerTests
{
    private readonly IKafkaProducer _producerMock;
    private readonly IConfiguration _configMock;
    private readonly ILogger<KafkaConsumer> _loggerMock;
    private readonly KafkaConsumer _sut;
    private readonly ConsumerConfig _consumerConfig;

    // We can't easily mock the sealed ConsumerBuilder or IConsumer directly with NSub due to extension methods / internal logic.
    // However, the current KafkaConsumer relies on real Confluent.Kafka classes. To test robustly without a real broker,
    // we ideally need to refactor KafkaConsumer to accept an IConsumer factory or abstraction.
    // FOR THIS TASK: We will assume we can refactor KafkaConsumer to be testable, or we use integration tests.
    // But since "TDD" was requested unit-style, I will create a testable seam.
    
    // STRATEGY: Refactor KafkaConsumer to take an `IConsumerFactory`.
    // But first, let's write the test assuming we have control over the consumption loop.
    // Since `ConsumeAsync` builds the consumer internally, we must extract that.
    
    // To proceed with TDD without massive refactoring, I'll extract the "Processing Logic" into a method 
    // `ProcessMessageAsync` or similar that takes a `ConsumeResult` and handles the retry/DLQ loop.
    // This allows unit testing the critical logic without spinning up a real Kafka consumer loop.

    public KafkaConsumerTests()
    {
        _producerMock = Substitute.For<IKafkaProducer>();
        _loggerMock = Substitute.For<ILogger<KafkaConsumer>>();
        
        // Use real configuration instead of mock to avoid extension method issues
        var myConfiguration = new Dictionary<string, string>
        {
            {"Kafka:DLQ:Enabled", "true"},
            {"Kafka:DLQ:TopicSuffix", ".DLQ"},
            {"Kafka:DLQ:MaxAttempts", "3"},
            {"Kafka:DLQ:BackoffBaseMs", "10"}, // Faster for tests
            {"Kafka:DLQ:BackoffMaxMs", "100"}
        };

        _configMock = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();
        
        _sut = new KafkaConsumer(_configMock, _loggerMock, _producerMock);
    }

    [Fact]
    public async Task ProcessMessage_Should_Commit_On_Success()
    {
        // Arrange
        var message = new ConsumeResult<string, string>
        {
            Topic = "test-topic",
            Partition = 0,
            Offset = 100,
            Message = new Message<string, string> { Key = "key", Value = "{\"Foo\":\"Bar\"}" }
        };

        bool handlerCalled = false;
        Func<TestMessage, CancellationToken, Task> handler = (msg, ct) =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        };

        // Act
        // We expose the internal logic for testing or use a Testable subclass if protected
        var result = await _sut.ProcessMessageInternal(message, handler, CancellationToken.None);

        // Assert
        handlerCalled.Should().BeTrue();
        result.ShouldCommit.Should().BeTrue("Should commit on success");
        await _producerMock.DidNotReceiveWithAnyArgs().PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
    }

    [Fact]
    public async Task ProcessMessage_Should_Retry_And_Commit_On_Eventual_Success()
    {
        // Arrange
        var message = new ConsumeResult<string, string>
        {
            Topic = "test-topic",
            Partition = 0,
            Offset = 101,
            Message = new Message<string, string> { Key = "key", Value = "{}" }
        };

        int attempts = 0;
        Func<TestMessage, CancellationToken, Task> handler = (msg, ct) =>
        {
            attempts++;
            if (attempts < 2) throw new Exception("Transient error");
            return Task.CompletedTask;
        };

        // Act
        var result = await _sut.ProcessMessageInternal(message, handler, CancellationToken.None);

        // Assert
        attempts.Should().Be(2);
        result.ShouldCommit.Should().BeTrue();
        await _producerMock.DidNotReceiveWithAnyArgs().PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
    }

    [Fact]
    public async Task ProcessMessage_Should_PublishToDLQ_And_Commit_On_PoisonMessage()
    {
        // Arrange
        var message = new ConsumeResult<string, string>
        {
            Topic = "test-topic",
            Partition = 0,
            Offset = 666,
            Message = new Message<string, string> 
            { 
                Key = "poison-key", 
                Value = "{\"poison\":true}",
                Headers = new Headers()
            }
        };

        Func<TestMessage, CancellationToken, Task> handler = (msg, ct) => throw new Exception("Permanent error");

        // Act
        var result = await _sut.ProcessMessageInternal(message, handler, CancellationToken.None);

        // Assert
        // Logic should try 3 times (MaxAttempts)
        // Then publish to DLQ
        // Then return Commit=true
        
        await _producerMock.Received(1).PublishAsync(
            "test-topic.DLQ", 
            Arg.Any<string>(), // Key
            Arg.Is<DlqEnvelope>(e => 
                e.OriginalTopic == "test-topic" && 
                e.AttemptCount == 3 && 
                e.Error == "Permanent error" &&
                e.OriginalPayloadBase64 != null
            ), 
            Arg.Any<CancellationToken>()
        );

        result.ShouldCommit.Should().BeTrue("Should commit after safely offloading to DLQ");
    }

    [Fact]
    public async Task ProcessMessage_Should_Throw_And_NotCommit_On_DLQ_Failure()
    {
        // Arrange
        var message = new ConsumeResult<string, string>
        {
            Topic = "test-topic", 
            Message = new Message<string, string> { Value = "{}" }
        };

        Func<TestMessage, CancellationToken, Task> handler = (msg, ct) => throw new Exception("Fail handler");
        
        _producerMock.PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Kafka down"));

        // Act
        Func<Task> act = async () => await _sut.ProcessMessageInternal(message, handler, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Kafka down");
        // Implicitly: No commit happens because exception bubbles up
    }

    [Fact]
    public async Task ProcessMessage_Should_Handle_Deserialization_Error_By_Publishing_Raw_Payload()
    {
        // Arrange
        var rawBytes = Encoding.UTF8.GetBytes("INVALID JSON {");
        var message = new ConsumeResult<string, string>
        {
            Topic = "bad-json-topic",
            Message = new Message<string, string> { Value = "INVALID JSON {" } // Confluent client usually gives string if configured, or we mock bytes access
        };
        // Note: In real Connect implementation we access Message.Value (string) or raw bytes. 
        // Our current wrapper uses string. If deserialization fails, we pass the original string.

        // Act
        var result = await _sut.ProcessMessageInternal<TestMessage>(message, null!, CancellationToken.None);

        // Assert
        await _producerMock.Received(1).PublishAsync(
            "bad-json-topic.DLQ", 
            Arg.Any<string>(),
            Arg.Is<DlqEnvelope>(e => e.ErrorType.Contains("DeserializationException") && e.OriginalPayloadBase64 != null),
            Arg.Any<CancellationToken>()
        );
        result.ShouldCommit.Should().BeTrue();
    }
}

public class TestMessage 
{ 
    public string Foo { get; set; }
}
