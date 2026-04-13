using AdminApi.API.Endpoints;
using AdminApi.Infrastructure.Persistence;
using AdminApi.Infrastructure.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Structured Logging ──────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .WriteTo.Console(outputTemplate:
           "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"));

// ── Database (EF Core + PostgreSQL + pgvector) ───────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres not configured");

builder.Services.AddDbContext<AdminDbContext>(opts =>
    opts.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly("AdminApi")
                  .UseVector()
                  .MigrationsHistoryTable("__ef_migrations_history", "axon"))
        .UseSnakeCaseNamingConvention()
        .EnableDetailedErrors(builder.Environment.IsDevelopment())
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

// ── Security ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<AesEncryptionService>();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience not configured");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new()
        {
            ValidateIssuer           = true,
            ValidIssuer              = jwtIssuer,
            ValidateAudience         = true,
            ValidAudience            = jwtAudience,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── CQRS & Validation ─────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

// ── OpenAPI (native ASP.NET Core — powers Scalar UI) ──────────────────────────
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, context, ct) =>
    {
        doc.Info = new()
        {
            Title       = "AxonVoice OS — Admin API",
            Version     = "v1",
            Description = """
                          Tenant & agent profile management REST API for AxonVoice OS.

                          **Authentication**: Bearer JWT token required on all endpoints.
                          Obtain a token from the Gateway service.
                          """
        };
        return Task.CompletedTask;
    });
});

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["db"]);

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

// ── Auto-Migrate on Startup (Development only) ────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await db.Database.MigrateAsync();
}

// ── API Documentation ─────────────────────────────────────────────────────────
// OpenAPI JSON spec  → GET /openapi/v1.json
// Scalar API browser → GET /scalar/v1
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title  = "AxonVoice Admin API";
    options.Theme  = ScalarTheme.DeepSpace;
    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    options.AddPreferredSecuritySchemes("Bearer");
    options.AddHttpAuthentication("Bearer", http =>
    {
        http.Token = "YOUR_JWT_TOKEN_HERE";
    });
});

// ── Endpoints ────────────────────────────────────────────────────────────────
app.MapAgentProfileEndpoints();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("db")
});

await app.RunAsync();
