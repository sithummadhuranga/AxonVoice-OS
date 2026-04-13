using Axon.Contracts.Domain.Enums;
using StackExchange.Redis;

namespace Orchestrator.Session;

/// <summary>
/// Manages per-call session state in Redis.
/// Implements the State pattern — each WebSocket session has isolated state.
/// Key schema: "session:{sessionId}" (hash)
/// </summary>
public sealed class SessionStateManager
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SessionStateManager> _logger;
    private const string SessionKeyPrefix = "session:";

    public SessionStateManager(
        IConnectionMultiplexer redis,
        ILogger<SessionStateManager> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Initialises a new session entry in Redis when a WebSocket connects.</summary>
    public async Task InitialiseAsync(
        string sessionId,
        Guid tenantId,
        Guid profileId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var db = _redis.GetDatabase();
        var key = BuildKey(sessionId);

        var entries = new HashEntry[]
        {
            new("TenantId", tenantId.ToString()),
            new("ProfileId", profileId.ToString()),
            new("Language", ((int)Language.Unknown).ToString()),
            new("IsActive", "true"),
            new("StartedAtUtc", DateTimeOffset.UtcNow.ToString("O"))
        };

        await db.HashSetAsync(key, entries);
        await db.KeyExpireAsync(key, TimeSpan.FromHours(2), CommandFlags.FireAndForget);

        _logger.LogInformation(
            "Session {SessionId} initialised for TenantId={TenantId} ProfileId={ProfileId}",
            sessionId, tenantId, profileId);
    }

    /// <summary>
    /// Updates the detected language in Redis session.
    /// Called immediately after Whisper ASR returns a language token.
    /// </summary>
    public async Task SetLanguageAsync(string sessionId, Language language, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var db = _redis.GetDatabase();
        await db.HashSetAsync(BuildKey(sessionId), "Language", ((int)language).ToString());

        _logger.LogDebug("Session {SessionId} language updated to {Language}", sessionId, language);
    }

    /// <summary>Retrieves the current detected language for a session.</summary>
    public async Task<Language> GetLanguageAsync(string sessionId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var db = _redis.GetDatabase();
        var value = await db.HashGetAsync(BuildKey(sessionId), "Language");

        return value.HasValue && int.TryParse((string?)value, out var intVal)
            ? (Language)intVal
            : Language.Unknown;
    }

    /// <summary>Marks the session as terminated and sets a short TTL for cleanup.</summary>
    public async Task TerminateAsync(string sessionId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var db = _redis.GetDatabase();
        await db.HashSetAsync(BuildKey(sessionId), "IsActive", "false");
        await db.KeyExpireAsync(BuildKey(sessionId), TimeSpan.FromMinutes(5), CommandFlags.FireAndForget);

        _logger.LogInformation("Session {SessionId} terminated", sessionId);
    }

    private static string BuildKey(string sessionId) => $"{SessionKeyPrefix}{sessionId}";
}
