using Axon.Contracts.Domain.Enums;

namespace Axon.Contracts.Domain.Dtos;

/// <summary>Agent purpose DTO for cross-service communication.</summary>
/// <param name="PurposeId">Unique identifier mapping to the <see cref="Purposes"/> enum.</param>
/// <param name="Name">Human-readable name (e.g., "E-Commerce Order Taking").</param>
/// <param name="BaseSystemPrompt">Base LLM system prompt injected for this purpose.</param>
/// <param name="WebhookUrl">Optional external API URL for custom webhook purposes.</param>
public sealed record AgentPurposeDto(
    Purposes PurposeId,
    string Name,
    string BaseSystemPrompt,
    string? WebhookUrl
);
