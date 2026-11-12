using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Game.Data;

public sealed class TelemetryDbContextFactory : IDesignTimeDbContextFactory<TelemetryDbContext>
{
    public TelemetryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__Database before running Entity Framework design-time commands.");
        }

        var options = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__TelemetryMigrationsHistory", TelemetryDbContext.Schema))
            .Options;

        return new TelemetryDbContext(options);
    }
}
