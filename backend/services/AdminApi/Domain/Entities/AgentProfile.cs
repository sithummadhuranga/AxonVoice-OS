using Axon.Contracts.Domain.Enums;

namespace AdminApi.Domain.Entities;

/// <summary>
/// EF Core entity — persisted in PostgreSQL `AgentProfiles` table.
/// Represents a tenant's configured voice agent.
/// </summary>
public sealed class AgentProfile
{
    public Guid ProfileId { get; init; } = Guid.NewGuid();
    public Guid TenantId { get; init; }
    public required string AgentName { get; set; }
    public Purposes PurposeId { get; set; }

    /// <summary>
    /// Optional webhook URL for custom integrations with the tenant's CRM.
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// AES-256 encrypted auth token for the webhook.
    /// Stored encrypted at rest — decrypted in-memory by AgentService only.
    /// </summary>
    public string? WebhookAuthTokenEncrypted { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public AgentPurpose? Purpose { get; set; }
}
