using AdminApi.Domain.Entities;
using AdminApi.Infrastructure.Persistence;
using AdminApi.Infrastructure.Security;
using Axon.Contracts.Domain.Dtos;
using Axon.Contracts.Domain.Enums;
using FluentValidation;
using MediatR;

namespace AdminApi.Application.Commands;

public sealed record CreateAgentProfileCommand(
    Guid TenantId,
    string AgentName,
    Purposes PurposeId,
    string? WebhookUrl,
    string? WebhookAuthToken) : IRequest<AgentProfileDto>;

public sealed class CreateAgentProfileValidator : AbstractValidator<CreateAgentProfileCommand>
{
    public CreateAgentProfileValidator()
    {
        RuleFor(x => x.AgentName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.WebhookUrl).MaximumLength(2048).When(x => !string.IsNullOrEmpty(x.WebhookUrl));
    }
}

public sealed class CreateAgentProfileHandler(
    AdminDbContext db,
    AesEncryptionService encryptionService)
    : IRequestHandler<CreateAgentProfileCommand, AgentProfileDto>
{
    public async Task<AgentProfileDto> Handle(CreateAgentProfileCommand request, CancellationToken ct)
    {
        var profile = new AgentProfile
        {
            TenantId = request.TenantId,
            AgentName = request.AgentName,
            PurposeId = request.PurposeId,
            WebhookUrl = request.WebhookUrl,
            WebhookAuthTokenEncrypted = string.IsNullOrEmpty(request.WebhookAuthToken) 
                ? null 
                : encryptionService.Encrypt(request.WebhookAuthToken)
        };

        db.AgentProfiles.Add(profile);
        await db.SaveChangesAsync(ct);

        return new AgentProfileDto(
            profile.ProfileId,
            profile.TenantId,
            profile.AgentName,
            profile.PurposeId,
            profile.WebhookUrl,
            profile.IsActive,
            profile.CreatedAtUtc);
    }
}
