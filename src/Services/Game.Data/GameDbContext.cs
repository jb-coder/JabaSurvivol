using Microsoft.EntityFrameworkCore;

namespace Game.Data;

public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public const string Schema = "game";

    public DbSet<CloudSave> CloudSaves => Set<CloudSave>();

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
    }
}
