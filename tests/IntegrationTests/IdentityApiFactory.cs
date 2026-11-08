using Identity.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JabaSurvivol.IntegrationTests;

public sealed class IdentityApiFactory(string connectionString, int authPermitLimit = 1_000)
    : WebApplicationFactory<Identity.Api.IdentityApiMarker>
{
    public const string SigningKey = "integration-test-signing-key-0123456789abcdef";
    public const string Issuer = "JabaSurvivol.Tests";
    public const string Audience = "JabaSurvivol.Tests";

    public string ConnectionString { get; } = connectionString;

    public IdentityApiFactory ApplyEnvironment()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Database", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
        Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenDays", "30");
        Environment.SetEnvironmentVariable("Database__Migrate", "false");
        Environment.SetEnvironmentVariable("RateLimiting__AuthPermitLimit", authPermitLimit.ToString());
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = ConnectionString,
                ["Jwt:SigningKey"] = SigningKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "30",
                ["Database:Migrate"] = "false",
                ["RateLimiting:AuthPermitLimit"] = authPermitLimit.ToString()
            });
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await database.Database.MigrateAsync();
    }
}
