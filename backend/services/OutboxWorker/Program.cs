using MassTransit;
using Microsoft.EntityFrameworkCore;
using OutboxWorker.Persistence;
using Serilog;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

// ── Structured Logging ──────────────────────────────────────────────────────
builder.Services.AddSerilog((_, cfg) =>
    cfg.ReadFrom.Configuration(builder.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console(outputTemplate:
           "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"));

// ── Database (OutboxWorker-owned DbContext) ───────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres not configured");

builder.Services.AddDbContext<OutboxDbContext>(opts =>
    opts.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());

// ── MassTransit / RabbitMQ ────────────────────────────────────────────────────
var rabbitMqUri = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMQ not configured");

builder.Services.AddMassTransit(mt =>
{
    mt.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(new Uri(rabbitMqUri));
        cfg.ConfigureEndpoints(ctx);
    });
});

// ── Outbox Publisher Worker ─────────────────────────────────────────────────
builder.Services.AddHostedService<OutboxPublisherWorker>();

var host = builder.Build();
await host.RunAsync();

// ─────────────────────────────────────────────────────────────────────────────
// Outbox Publisher — polls DB, publishes events, marks as processed
// ─────────────────────────────────────────────────────────────────────────────

internal sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<OutboxPublisherWorker> _logger;
    private readonly TimeSpan _pollInterval;

    public OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        IPublishEndpoint publisher,
        ILogger<OutboxPublisherWorker> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _publisher    = publisher    ?? throw new ArgumentNullException(nameof(publisher));
        _logger       = logger       ?? throw new ArgumentNullException(nameof(logger));

        var intervalSeconds = int.Parse(config["Outbox:PollIntervalSeconds"] ?? "5");
        _pollInterval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxPublisherWorker started. Poll interval: {Interval}s", _pollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during outbox processing cycle");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();

        const int batchSize = 50;

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await PublishMessageAsync(message, ct);
                message.ProcessedAtUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish outbox message {Id} of type {Type}",
                    message.Id, message.EventType);
                message.RetryCount++;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task PublishMessageAsync(OutboxMessage message, CancellationToken ct)
    {
        var eventType = Type.GetType(message.EventType)
            ?? throw new InvalidOperationException(
                $"Cannot resolve CLR type for event: {message.EventType}");

        var payload = JsonSerializer.Deserialize(message.Payload, eventType)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize outbox payload for event {message.Id}");

        await _publisher.Publish(payload, eventType, ct);

        _logger.LogDebug(
            "Published outbox event {Id} ({Type}) to topic {Topic}",
            message.Id, message.EventType, message.Topic);
    }
}
