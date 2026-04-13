namespace AdminApi.Domain.Entities;

/// <summary>
/// Transactional Outbox table entity.
/// Every domain event is written here atomically in the same EF transaction as the business operation.
/// The <see cref="OutboxWorker"/> polls this table, publishes unprocessed events to RabbitMQ,
/// then marks them as <see cref="ProcessedAtUtc"/>.
///
/// Pattern: Transactional Outbox — guarantees at-least-once delivery with idempotency.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Idempotency key. Consumers check this GUID to avoid double-processing.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Fully qualified CLR type name of the event (e.g. "Axon.Contracts.Domain.Events.OrderCreatedEvent").</summary>
    public required string EventType { get; init; }

    /// <summary>JSON-serialized event payload.</summary>
    public required string Payload { get; init; }

    /// <summary>RabbitMQ exchange or topic to publish to.</summary>
    public required string Topic { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Null until successfully published to RabbitMQ.</summary>
    public DateTimeOffset? ProcessedAtUtc { get; set; }

    /// <summary>Tracks retry attempts for observability and alerting.</summary>
    public int RetryCount { get; set; }
}
