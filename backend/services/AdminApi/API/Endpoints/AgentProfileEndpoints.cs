using AdminApi.Domain.Entities;
using AdminApi.Infrastructure.Persistence;
using Axon.Contracts.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.API.Endpoints;

/// <summary>
/// Minimal API endpoints for AgentProfile CRUD.
/// Registered via <see cref="MapAgentProfileEndpoints"/> extension method pattern.
/// Routes: /admin/profiles/...
/// </summary>
public static class AgentProfileEndpoints
{
    public static void MapAgentProfileEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/admin/profiles")
                          .RequireAuthorization()
                          .WithTags("Agent Profiles");

        // GET /admin/profiles/{tenantId}
        group.MapGet("/{tenantId:guid}", GetProfilesByTenantAsync)
             .WithName("GetAgentProfiles")
             .WithDescription("Returns all agent profiles for a given tenant.");

        // GET /admin/profiles/detail/{profileId}
        group.MapGet("/detail/{profileId:guid}", GetProfileByIdAsync)
             .WithName("GetAgentProfileById");

        // POST /admin/profiles
        group.MapPost("/", CreateProfileAsync)
             .WithName("CreateAgentProfile");

        // PUT /admin/profiles/{profileId}
        group.MapPut("/{profileId:guid}", UpdateProfileAsync)
             .WithName("UpdateAgentProfile");

        // DELETE /admin/profiles/{profileId}
        group.MapDelete("/{profileId:guid}", DeleteProfileAsync)
             .WithName("DeleteAgentProfile");
    }

    private static async Task<IResult> GetProfilesByTenantAsync(
        Guid tenantId,
        AdminDbContext db,
        CancellationToken ct)
    {
        var profiles = await db.AgentProfiles
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .Include(p => p.Purpose)
            .ToListAsync(ct);

        return Results.Ok(profiles);
    }

    private static async Task<IResult> GetProfileByIdAsync(
        Guid profileId,
        AdminDbContext db,
        CancellationToken ct)
    {
        var profile = await db.AgentProfiles
            .AsNoTracking()
            .Include(p => p.Purpose)
            .FirstOrDefaultAsync(p => p.ProfileId == profileId, ct);

        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static async Task<IResult> CreateProfileAsync(
        CreateProfileRequest request,
        MediatR.ISender sender,
        CancellationToken ct)
    {
        var command = new AdminApi.Application.Commands.CreateAgentProfileCommand(
            request.TenantId,
            request.AgentName,
            request.PurposeId,
            request.WebhookUrl,
            request.WebhookAuthToken);

        var result = await sender.Send(command, ct);

        return Results.CreatedAtRoute("GetAgentProfileById",
            new { profileId = result.ProfileId }, result);
    }

    private static async Task<IResult> UpdateProfileAsync(
        Guid profileId,
        UpdateProfileRequest request,
        AdminDbContext db,
        CancellationToken ct)
    {
        var profile = await db.AgentProfiles.FindAsync([profileId], ct);

        if (profile is null)
            return Results.NotFound();

        profile.AgentName = request.AgentName;
        profile.PurposeId = request.PurposeId;
        profile.IsActive = request.IsActive;
        profile.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteProfileAsync(
        Guid profileId,
        AdminDbContext db,
        CancellationToken ct)
    {
        var deleted = await db.AgentProfiles
            .Where(p => p.ProfileId == profileId)
            .ExecuteDeleteAsync(ct);

        return deleted > 0 ? Results.NoContent() : Results.NotFound();
    }

    // ── Request Models ───────────────────────────────────────────────────────

    private sealed record CreateProfileRequest(
        Guid TenantId,
        string AgentName,
        Purposes PurposeId,
        string? WebhookUrl,
        string? WebhookAuthToken);

    private sealed record UpdateProfileRequest(
        string AgentName,
        Purposes PurposeId,
        bool IsActive);
}
