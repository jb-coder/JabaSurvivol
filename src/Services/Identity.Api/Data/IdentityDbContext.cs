using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Data;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public const string Schema = "identity";

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Account>(account =>
        {
            account.ToTable("Accounts", Schema);
            account.HasKey(x => x.Id);
            account.Property(x => x.UserName).HasMaxLength(32).IsRequired();
            account.Property(x => x.NormalizedUserName).HasMaxLength(32).IsRequired();
            account.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            account.Property(x => x.CreatedAt).IsRequired();
            account.HasIndex(x => x.NormalizedUserName).IsUnique();
        });

        modelBuilder.Entity<RefreshSession>(session =>
        {
            session.ToTable("RefreshSessions", Schema);
            session.HasKey(x => x.Id);
            session.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            session.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
            session.Property(x => x.CreatedAt).IsRequired();
            session.Property(x => x.ExpiresAt).IsRequired();
            session.HasIndex(x => x.TokenHash).IsUnique();
            session.HasIndex(x => x.AccountId);
            session.HasIndex(x => x.FamilyId);
            session.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
