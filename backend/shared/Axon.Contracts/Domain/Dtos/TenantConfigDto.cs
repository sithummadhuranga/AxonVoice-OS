namespace Axon.Contracts.Domain.Dtos;

/// <summary>
/// Tenant runtime configuration DTO cached in Redis.
/// Read by the Orchestrator and AgentService on each call to configure the pipeline.
/// </summary>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="AgentProfile">The active agent profile for this tenant's session.</param>
/// <param name="AgentPurpose">The resolved purpose (includes system prompt).</param>
/// <param name="WebhookAuthTokenEncrypted">
///   AES-256 encrypted auth token for custom webhook calls.
///   Decrypted in-memory by AgentService only — never passed over the wire decrypted.
/// </param>
public sealed record TenantConfigDto(
    Guid TenantId,
    AgentProfileDto AgentProfile,
    AgentPurposeDto AgentPurpose,
    string? WebhookAuthTokenEncrypted
);
