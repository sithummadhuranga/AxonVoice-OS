using Axon.Contracts.Domain.Enums;

namespace Axon.Contracts.Domain.Dtos;

/// <summary>Agent profile DTO for cross-service communication (non-EF model).</summary>
/// <param name="ProfileId">Unique identifier for this agent configuration.</param>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="AgentName">Display name of the agent shown in the dashboard.</param>
/// <param name="PurposeId">Determines which Semantic Kernel plugins are mounted.</param>
/// <param name="IsActive">Whether this agent profile is live and accepting calls.</param>
public sealed record AgentProfileDto(
    Guid ProfileId,
    Guid TenantId,
    string AgentName,
    Purposes PurposeId,
    bool IsActive
);
