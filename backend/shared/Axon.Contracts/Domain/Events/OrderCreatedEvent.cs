using Axon.Contracts.Domain.Enums;

namespace Axon.Contracts.Domain.Events;

/// <summary>
/// Domain event published to RabbitMQ via the Outbox Pattern when an order is
/// successfully placed by the OrderManagementPlugin.
/// Consumers: Billing Service, Shipping Service.
/// </summary>
/// <param name="EventId">Idempotency key — deduplicated by consumers using this GUID.</param>
/// <param name="OccurredAtUtc">UTC timestamp of when the order was created in the database.</param>
/// <param name="OrderId">Primary key of the created order record.</param>
/// <param name="TenantId">Tenant who owns the agent that placed this order.</param>
/// <param name="SessionId">WebSocket session ID for correlation and debugging.</param>
/// <param name="CustomerName">Customer name extracted by the LLM during conversation.</param>
/// <param name="Items">Ordered line items — immutable snapshot at time of placement.</param>
/// <param name="TotalAmountCents">Total order amount in smallest currency unit (cents).</param>
public sealed record OrderCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid OrderId,
    Guid TenantId,
    string SessionId,
    string CustomerName,
    IReadOnlyList<OrderLineItem> Items,
    long TotalAmountCents
);

/// <summary>Immutable line item snapshot within an <see cref="OrderCreatedEvent"/>.</summary>
/// <param name="ProductId">Product identifier from the tenant's catalog.</param>
/// <param name="ProductName">Human-readable name at time of order.</param>
/// <param name="Quantity">Units ordered.</param>
/// <param name="UnitPriceCents">Price per unit in cents at time of order.</param>
public sealed record OrderLineItem(
    Guid ProductId,
    string ProductName,
    int Quantity,
    long UnitPriceCents
);
