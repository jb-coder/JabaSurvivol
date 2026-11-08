using Microsoft.EntityFrameworkCore;

namespace Game.Data;

public sealed class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : DbContext(options)
{
    public const string Schema = "telemetry";

    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();
    public DbSet<TelemetryDailyAggregate> DailyAggregates => Set<TelemetryDailyAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<TelemetryEvent>(telemetryEvent =>
        {
            telemetryEvent.ToTable("TelemetryEvents", Schema);
            telemetryEvent.HasKey(x => x.Id);
            telemetryEvent.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            telemetryEvent.Property(x => x.Payload).HasColumnType("text").IsRequired();
            telemetryEvent.Property(x => x.OccurredAt).IsRequired();
            telemetryEvent.Property(x => x.ReceivedAt).IsRequired();
            telemetryEvent.HasIndex(x => new { x.AccountId, x.EventId }).IsUnique();
            telemetryEvent.HasIndex(x => x.OccurredAt);
        });

        modelBuilder.Entity<TelemetryDailyAggregate>(aggregate =>
        {
            aggregate.ToTable("DailyAggregates", Schema);
            aggregate.HasKey(x => x.Date);
            aggregate.Property(x => x.UpdatedAt).IsRequired();
        });
    }
}
