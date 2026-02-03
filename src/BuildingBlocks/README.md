# Kafka BuildingBlocks - Dead Letter Queue (DLQ)

This library implements a strict DLQ pattern to handle poison messages without data loss.

## DLQ Logic
- **Retries**: Configurable exponential backoff + jitter.
- **Failures**: After `MaxAttempts`, messages are wrapped in a `DlqEnvelope` and sent to `<OriginalTopic>.DLQ`.
- **Safety**: If publishing to DLQ fails, the consumer **CRASHES**. This prevents the original offset from being committed, ensuring "at-least-once" semantics.

## Configuration
Ensure these settings are in `appsettings.json`:
```json
"Kafka": {
  "BootstrapServers": "localhost:9092",
  "GroupId": "my-service-group",
  "DLQ": {
    "Enabled": true,
    "TopicSuffix": ".DLQ",
    "MaxAttempts": 5
  }
}
```

## Critical Requirements
> [!IMPORTANT]
> **DLQ TOPICS MUST EXIST**
> The consumer will **CRASH** if it cannot publish to the DLQ topic. Ensure your infrastructure (Terraform/Helm) creates `<Topic>.DLQ` or that your cluster permits auto-creation.

> [!WARNING]
> **MESSAGE SIZE**
> DLQ payloads include the original message as base64, increasing size by ~33%. Ensure `message.max.bytes` on your brokers is sufficient.

> [!CAUTION]
> **DISABLING DLQ**
> Setting `Kafka:DLQ:Enabled=false` causes poison messages to be **DISCARDED (Committed)** without processing. Only use this if dataloss is acceptable for bad messages.

## Safe Replay Story
To replay messages from the DLQ:

1. **Consume** from the `<OriginalTopic>.DLQ`.
   * *Note*: The Kafka Message Key is the deterministic `DlqMessageId` (Hash of Topic-Partition-Offset). Use this to deduplicate replays.
2. **Read Envelope**: Parse the `DlqEnvelope` JSON.
3. **Extract Payload**: Decode `OriginalPayloadBase64` to bytes.
4. **Resubmit**: Verify/Fix the payload and publish it back to `OriginalTopic`.
   * *Tip*: You may want to strip/reset headers to avoid immediate re-DLQing if headers caused the issue.

### Manual Replay Script (Example)
```csharp
var envelope = JsonSerializer.Deserialize<DlqEnvelope>(consumedValue);
var originalBytes = Convert.FromBase64String(envelope.OriginalPayloadBase64);
// ... Fix issue ...
await producer.ProduceAsync(envelope.OriginalTopic, new Message<string, byte[]> { Value = originalBytes });
```
