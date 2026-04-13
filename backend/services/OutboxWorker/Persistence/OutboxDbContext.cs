using Microsoft.EntityFrameworkCore;

namespace OutboxWorker.Persistence;

/// <summary>
/// Lightweight read/write DbContext for the OutboxWorker.
/// Only maps the outbox_messages table — no entity ownership outside this boundary.
/// The axon schema is shared with AdminApi but accessed independently here.
/// </summary>
public sealed class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("axon");

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(o => o.Id);
            b.HasIndex(o => o.ProcessedAtUtc);
            b.Property(o => o.EventType).HasMaxLength(256).IsRequired();
            b.Property(o => o.Topic).HasMaxLength(256).IsRequired();
            b.Property(o => o.Payload).IsRequired();
        });
    }
}

/// <summary>
/// Local entity — mirrors AdminApi's OutboxMessage schema.
/// Kept here to eliminate the AdminApi project reference.
/// </summary>
public sealed class OutboxMessage
{
    public Guid              Id              { get; init;  } = Guid.NewGuid();
    public string            EventType       { get; init;  } = string.Empty;
    public string            Topic           { get; init;  } = string.Empty;
    public string            Payload         { get; init;  } = string.Empty;
    public DateTimeOffset    CreatedAtUtc    { get; init;  } = DateTimeOffset.UtcNow;
    public DateTimeOffset?   ProcessedAtUtc  { get; set;   }
    public int               RetryCount      { get; set;   }
}
