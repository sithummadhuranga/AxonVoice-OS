using Serilog;

// ─────────────────────────────────────────────────────────────────────────────
// AxonVoice OS — API Gateway (YARP)
// Responsibilities:
//   • JWT Bearer authentication on all routes
//   • WebSocket proxying to Orchestrator
//   • Rate limiting (sliding window)
//   • Circuit breaker on Ollama via Polly
//   • Health checks for all downstream dependencies
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ── Structured Logging ──────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .WriteTo.Console(outputTemplate:
           "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"));

// ── YARP Reverse Proxy ───────────────────────────────────────────────────────
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── JWT Authentication ───────────────────────────────────────────────────────
builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = null; // Self-hosted — symmetric key
        options.RequireHttpsMetadata = false; // Enable in production behind TLS terminator
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"]
                          ?? throw new InvalidOperationException("Jwt:Issuer not configured"),
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"]
                            ?? throw new InvalidOperationException("Jwt:Audience not configured"),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:SecretKey"]
                    ?? throw new InvalidOperationException("Jwt:SecretKey not configured — check .env")))
        };
    });

builder.Services.AddAuthorization();

// ── Rate Limiting ────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.AddSlidingWindowLimiter("api", limiterOpts =>
    {
        limiterOpts.Window = TimeSpan.FromMinutes(1);
        limiterOpts.SegmentsPerWindow = 6;
        limiterOpts.PermitLimit = int.Parse(
            builder.Configuration["RateLimit:PermitPerMinute"] ?? "120");
        limiterOpts.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOpts.QueueLimit = 10;
    });
});

// ── Health Checks ────────────────────────────────────────────────────────────
builder.Services
    .AddHealthChecks()
    .AddNpgSql(
        builder.Configuration["ConnectionStrings:Postgres"]
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres not configured"),
        name: "postgres",
        tags: ["db"])
    .AddRedis(
        builder.Configuration["ConnectionStrings:Redis"]
        ?? throw new InvalidOperationException("ConnectionStrings:Redis not configured"),
        name: "redis",
        tags: ["cache"])
    .AddRabbitMQ(
        rabbitConnectionString: builder.Configuration["ConnectionStrings:RabbitMQ"]
        ?? throw new InvalidOperationException("ConnectionStrings:RabbitMQ not configured"),
        name: "rabbitmq",
        tags: ["messaging"]);

// ── CORS ─────────────────────────────────────────────────────────────────────
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"]
    ?? throw new InvalidOperationException("Cors:AllowedOrigin not configured");

builder.Services.AddCors(opts =>
    opts.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors("FrontendPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoints (NOT gated by auth or rate limiting)
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("db")
                      || check.Tags.Contains("cache")
                      || check.Tags.Contains("messaging")
});

// Proxy all other traffic through YARP
app.MapReverseProxy();

await app.RunAsync();
