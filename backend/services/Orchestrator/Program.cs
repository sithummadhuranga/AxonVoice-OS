using Orchestrator.Audio;
using Orchestrator.Session;
using Orchestrator.Workers;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Structured Logging ──────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .WriteTo.Console());

// ── Redis ────────────────────────────────────────────────────────────────────
var redisConnectionString = builder.Configuration["ConnectionStrings:Redis"]
    ?? throw new InvalidOperationException("ConnectionStrings:Redis not configured");

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));

// ── Core Services ────────────────────────────────────────────────────────────
builder.Services.AddSingleton<SessionStateManager>();
builder.Services.AddSingleton<FillerAudioProvider>();
builder.Services.AddHostedService<VoiceOrchestratorWorker>();
builder.Services.AddSingleton<VoiceOrchestratorWorker>();

// ── Resilient HTTP Client for Ollama ASR ─────────────────────────────────────
builder.Services.AddHttpClient("OllamaAsr", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Ollama:BaseUrl"]
        ?? throw new InvalidOperationException("Ollama:BaseUrl not configured"));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler();

// ── Health Checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddRedis(redisConnectionString, name: "redis");

var app = builder.Build();

app.UseSerilogRequestLogging();

// ── WebSocket Support ────────────────────────────────────────────────────────
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

app.Map("/ws/voice/{tenantId}/{profileId}", async (
    HttpContext ctx,
    string tenantId,
    string profileId,
    VoiceOrchestratorWorker worker,
    CancellationToken ct) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    if (!Guid.TryParse(tenantId, out var tenantGuid) || !Guid.TryParse(profileId, out var profileGuid))
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    await worker.HandleSessionAsync(ws, tenantGuid, profileGuid, ct);
});

app.MapHealthChecks("/health");

await app.RunAsync();
