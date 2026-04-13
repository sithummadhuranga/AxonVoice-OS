using AdminApi.API.Endpoints;
using AdminApi.Infrastructure.Persistence;
using AdminApi.Infrastructure.Security;
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
       .WriteTo.Console());

// ── Database (EF Core + PostgreSQL + Code-First) ────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres not configured");

builder.Services.AddDbContext<AdminDbContext>(opts =>
    opts.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly("AdminApi"))
        .UseSnakeCaseNamingConvention()
        .EnableDetailedErrors(builder.Environment.IsDevelopment())
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

// ── Security ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<AesEncryptionService>();

// ── JWT Authentication ───────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new()
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
                    ?? throw new InvalidOperationException("Jwt:SecretKey not configured")))
        };
    });

builder.Services.AddAuthorization();

// ── OpenAPI ───────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Health Checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

// ── Auto-Migrate on Startup (Development) ────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await db.Database.MigrateAsync();
}

// ── OpenAPI / Scalar UI ───────────────────────────────────────────────────────
app.MapOpenApi();
app.MapScalarApiReference();

// ── Endpoints ────────────────────────────────────────────────────────────────
app.MapAgentProfileEndpoints();
app.MapHealthChecks("/health");

await app.RunAsync();
