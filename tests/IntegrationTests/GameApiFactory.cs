using System.Security.Cryptography;
using System.Text;
using Game.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JabaSurvivol.IntegrationTests;

public sealed class GameApiFactory(string connectionString, int leaderboardPermitLimit = 1_000) : WebApplicationFactory<GameApiMarker>
{
    public const string Issuer = "JabaSurvivol.Tests";
    public const string Audience = "JabaSurvivol.Tests";

    public string ConnectionString { get; } = connectionString;
    public string SigningKey { get; } = "jaba-game-" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public GameApiFactory ApplyEnvironment()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Database", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
        Environment.SetEnvironmentVariable("Database__Migrate", "false");
        Environment.SetEnvironmentVariable("RateLimiting__LeaderboardSubmitPermitLimit", leaderboardPermitLimit.ToString());
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
                ["Database:Migrate"] = "false",
                ["RateLimiting:LeaderboardSubmitPermitLimit"] = leaderboardPermitLimit.ToString()
            });
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        await database.Database.MigrateAsync();

        var telemetry = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
        await telemetry.Database.MigrateAsync();
    }

    public HttpClient CreateClientFor(Guid accountId, string userName = "GameTester")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            TestTokenFactory.Create(accountId, SigningKey, Issuer, Audience, userName: userName));
        return client;
    }
}

public static class TestTokenFactory
{
    public static string Create(
        Guid accountId,
        string signingKey,
        string issuer,
        string audience,
        DateTime? expires = null,
        string userName = "GameTester")
    {
        var expiration = expires ?? DateTime.UtcNow.AddMinutes(15);
        var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = expiration.AddMinutes(-15),
            Expires = expiration,
            Subject = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim("sub", accountId.ToString()),
                new System.Security.Claims.Claim("username", userName)
            ]),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)
        };

        return new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().CreateToken(descriptor);
    }
}
