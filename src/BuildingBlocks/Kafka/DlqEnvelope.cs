using System.Text.Json.Serialization;

namespace BuildingBlocks.Kafka;

public sealed class DlqEnvelope
{
    public int SchemaVersion { get; init; } = 1;
    public string OriginalTopic { get; init; } = default!;
    public int OriginalPartition { get; init; }
    public long OriginalOffset { get; init; }
    public string? ConsumerGroup { get; init; }

    public string? OriginalKeyBase64 { get; init; }
    public Dictionary<string, string>? OriginalHeaders { get; init; } 
    public string OriginalPayloadBase64 { get; init; } = default!;

    public string Error { get; init; } = default!;
    public string? ErrorType { get; init; }
    public string? StackTrace { get; init; }
    public DateTimeOffset FailedAtUtc { get; init; }
    public int AttemptCount { get; init; }

    public string DlqMessageId { get; init; } = default!; // Topic-Partition-Offset hash
}
