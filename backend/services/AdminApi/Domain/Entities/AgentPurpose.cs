using Axon.Contracts.Domain.Enums;

namespace AdminApi.Domain.Entities;

/// <summary>
/// EF Core entity — persisted in PostgreSQL `AgentPurposes` table.
/// Defines a reusable purpose configuration that agents can adopt.
/// </summary>
public sealed class AgentPurpose
{
    public Purposes PurposeId { get; init; }
    public required string Name { get; set; }
    public required string BaseSystemPrompt { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<AgentProfile> Profiles { get; set; } = [];
}
