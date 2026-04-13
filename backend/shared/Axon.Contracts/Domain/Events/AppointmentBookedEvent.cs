namespace Axon.Contracts.Domain.Events;

/// <summary>
/// Domain event published to RabbitMQ via the Outbox Pattern when a clinic
/// appointment is successfully booked by the CalendarBookingPlugin.
/// Consumers: Notification Service, Calendar Sync Service.
/// </summary>
/// <param name="EventId">Idempotency key — deduplicated by consumers using this GUID.</param>
/// <param name="OccurredAtUtc">UTC timestamp of booking creation.</param>
/// <param name="AppointmentId">Primary key of the created appointment record.</param>
/// <param name="TenantId">Tenant who owns the agent that made this booking.</param>
/// <param name="SessionId">WebSocket session ID for correlation.</param>
/// <param name="PatientName">Patient name extracted by the LLM.</param>
/// <param name="SlotUtc">UTC datetime of the booked appointment slot.</param>
/// <param name="DurationMinutes">Duration of the appointment in minutes.</param>
/// <param name="ServiceType">Type of service/reason for visit.</param>
public sealed record AppointmentBookedEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid AppointmentId,
    Guid TenantId,
    string SessionId,
    string PatientName,
    DateTimeOffset SlotUtc,
    int DurationMinutes,
    string ServiceType
);
