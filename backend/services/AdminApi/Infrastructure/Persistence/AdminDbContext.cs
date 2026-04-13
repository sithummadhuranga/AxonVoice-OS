using AdminApi.Domain.Entities;
using Axon.Contracts.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the AdminApi.
/// Code-First: Migrations generate the schema.
/// pgvector extension is enabled in the initial migration via
/// <c>migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;")</c>
/// </summary>
public sealed class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<AgentProfile> AgentProfiles => Set<AgentProfile>();
    public DbSet<AgentPurpose> AgentPurposes => Set<AgentPurpose>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── AgentPurpose ─────────────────────────────────────────────────────
        modelBuilder.Entity<AgentPurpose>(b =>
        {
            b.HasKey(p => p.PurposeId);
            b.Property(p => p.PurposeId).HasConversion<int>();
            b.Property(p => p.Name).HasMaxLength(100).IsRequired();
            b.Property(p => p.BaseSystemPrompt).HasMaxLength(8000).IsRequired();
            b.Property(p => p.WebhookUrl).HasMaxLength(2048);
            b.Property(p => p.WebhookAuthTokenEncrypted).HasMaxLength(512);

            // Seed built-in purposes
            b.HasData(
                new AgentPurpose
                {
                    PurposeId = Purposes.OrderTaking,
                    Name = "E-Commerce Order Taking",
                    BaseSystemPrompt = "You are an order processing agent for a retail store. " +
                                       "Help customers check product availability and place orders. " +
                                       "Always confirm order details before placing.",
                    CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                new AgentPurpose
                {
                    PurposeId = Purposes.Booking,
                    Name = "Clinic Appointment Booking",
                    BaseSystemPrompt = "You are a receptionist scheduling medical appointments. " +
                                       "Help patients find available slots and book appointments. " +
                                       "Always confirm the patient's name, date, and service type.",
                    CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                new AgentPurpose
                {
                    PurposeId = Purposes.GeneralFaq,
                    Name = "General FAQ",
                    BaseSystemPrompt = "You are a helpful customer service agent. " +
                                       "Answer questions based on the provided knowledge base. " +
                                       "If you don't know the answer, say so honestly.",
                    CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
                }
            );
        });

        // ── AgentProfile ──────────────────────────────────────────────────────
        modelBuilder.Entity<AgentProfile>(b =>
        {
            b.HasKey(p => p.ProfileId);
            b.HasIndex(p => p.TenantId);
            b.Property(p => p.PurposeId).HasConversion<int>();
            b.Property(p => p.AgentName).HasMaxLength(150).IsRequired();

            b.HasOne(p => p.Purpose)
             .WithMany(pu => pu.Profiles)
             .HasForeignKey(p => p.PurposeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── OutboxMessage ─────────────────────────────────────────────────────
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.HasKey(o => o.Id);
            b.HasIndex(o => o.ProcessedAtUtc); // Partial index on unprocessed: WHERE ProcessedAtUtc IS NULL
            b.Property(o => o.EventType).HasMaxLength(256).IsRequired();
            b.Property(o => o.Topic).HasMaxLength(256).IsRequired();
            b.Property(o => o.Payload).IsRequired();
        });
    }
}
