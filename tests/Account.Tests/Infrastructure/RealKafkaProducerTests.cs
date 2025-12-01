using Confluent.Kafka;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;
using BuildingBlocks.Kafka;
using Infrastructure.Kafka;

public class RealKafkaProducerTests : IAsyncLifetime
{
    private readonly IContainer _rp;
    private const int HostKafkaPort = 19092;
    private string _bootstrap = default!;

    public RealKafkaProducerTests()
    {
        _rp = new ContainerBuilder()
            .WithImage("redpandadata/redpanda:latest")
            .WithPortBinding(HostKafkaPort, 9092)
            .WithCommand(
                "redpanda", "start",
                "--overprovisioned",
                "--smp", "1",
                "--memory", "512M",
                "--reserve-memory", "0M",
                "--node-id", "0",
                "--check=false",
                "--kafka-addr", "0.0.0.0:9092",
                "--advertise-kafka-addr", $"127.0.0.1:{HostKafkaPort}"
            )
            .WithWaitStrategy(Wait
                .ForUnixContainer()
                .UntilMessageIsLogged("Successfully started Redpanda!"))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _rp.StartAsync();
        _bootstrap = $"127.0.0.1:{HostKafkaPort}";
    }

    public async Task DisposeAsync() => await _rp.DisposeAsync();

    [Fact]
    public async Task Producer_Should_Send_And_Consumer_Should_Receive()
    {
        // Arrange
        var settings = new Dictionary<string, string> { ["Kafka:BootstrapServers"] = _bootstrap };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var kafkaProducer = new KafkaProducer(
            config, new NullLogger<KafkaProducer>());

        var producer = new AccountCreatedProducer(kafkaProducer);

        var topic = "test-topic";
        var message = new { Id = Guid.NewGuid(), Email = "user@test.dk" };

        // Act
        await producer.PublishAsync(topic, message);

        // Assert (consume)
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrap,
            GroupId = "tests",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(topic);

        var record = consumer.Consume(TimeSpan.FromSeconds(10));
        record.Should().NotBeNull("we should consume what we just produced");

        var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(record!.Message.Value);
        payload.Should().ContainKey("Email");
        payload!["Email"].ToString().Should().Be("user@test.dk");

        consumer.Close();
    }
}
