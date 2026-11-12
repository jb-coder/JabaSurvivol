using Microsoft.EntityFrameworkCore;

namespace Game.Data;

public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public const string Schema = "game";

    public DbSet<CloudSave> CloudSaves => Set<CloudSave>();
    public DbSet<LeaderboardRun> LeaderboardRuns => Set<LeaderboardRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<CloudSave>(save =>
        {
            save.ToTable("CloudSaves", Schema);
            save.HasKey(x => x.Id);
            save.Property(x => x.Slot).HasMaxLength(32).IsRequired();
            save.Property(x => x.Document).HasColumnType("text").IsRequired();
            save.Property(x => x.Revision).IsConcurrencyToken();
            save.HasIndex(x => new { x.AccountId, x.Slot }).IsUnique();
        });

        modelBuilder.Entity<LeaderboardRun>(run =>
        {
            run.ToTable("LeaderboardRuns", Schema);
            run.HasKey(x => x.Id);
            run.Property(x => x.Username).HasMaxLength(32).IsRequired();
            run.Property(x => x.ClientVersion).HasMaxLength(64).IsRequired();
            run.Property(x => x.AchievedAt).IsRequired();
            run.HasIndex(x => new { x.AccountId, x.RunId }).IsUnique();
            run.HasIndex(x => new { x.HighestRound, x.TotalKills, x.DurationSeconds, x.AchievedAt });
        });
    }
}
