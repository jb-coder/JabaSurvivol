using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JabaSurvivol.Contracts;

namespace JabaSurvivol.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class RemoteConfigEndpointTests(PostgresFixture fixture) : IAsyncLifetime
{
    private GameApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync("game_config");
        _factory = new GameApiFactory(connectionString).ApplyEnvironment();
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Config_returns_documented_defaults_and_cache_headers()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(ApiRoutes.GameConfig);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.Equal(TimeSpan.FromMinutes(1), response.Headers.CacheControl?.MaxAge);
        Assert.True(response.Headers.CacheControl?.Public);

        var config = await response.Content.ReadFromJsonAsync<RemoteConfigResponse>();
        Assert.NotNull(config);
        Assert.Equal(1, config.Version);
        Assert.Equal("0.5.0", config.MinimumClientVersion);
        Assert.False(config.MaintenanceMode);
        Assert.True(config.CloudSavesEnabled);
        Assert.True(config.LeaderboardsEnabled);
        Assert.False(config.TelemetryEnabled);
        Assert.Equal(0d, config.TelemetrySampleRate);
    }

    [Fact]
    public async Task Config_supports_etag_conditional_requests()
    {
        using var client = _factory.CreateClient();
        var first = await client.GetAsync(ApiRoutes.GameConfig);
        var etag = first.Headers.ETag;
        Assert.NotNull(etag);

        using var conditional = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.GameConfig);
        conditional.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag.Tag));
        var second = await client.SendAsync(conditional);

        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
    }

    [Fact]
    public async Task Config_contains_no_economy_or_user_data()
    {
        using var client = _factory.CreateClient();

        var json = await client.GetStringAsync(ApiRoutes.GameConfig);
        using var document = JsonDocument.Parse(json);
        var names = document.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("supplies", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("xp", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("level", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("skillPoints", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("cores", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("accountId", names, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public async Task Invalid_sample_rate_prevents_startup(string sampleRate)
    {
        Environment.SetEnvironmentVariable("RemoteConfig__TelemetrySampleRate", sampleRate);
        try
        {
            var connectionString = await fixture.CreateDatabaseAsync("game_config_invalid_rate");
            using var factory = new GameApiFactory(connectionString).ApplyEnvironment();

            var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
            Assert.Contains("RemoteConfig:TelemetrySampleRate", exception.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RemoteConfig__TelemetrySampleRate", null);
        }
    }

    [Fact]
    public async Task Etag_changes_when_configuration_changes()
    {
        using var client = _factory.CreateClient();
        var first = await client.GetAsync(ApiRoutes.GameConfig);
        var firstEtag = first.Headers.ETag?.Tag;
        Assert.NotNull(firstEtag);

        Environment.SetEnvironmentVariable("RemoteConfig__MaintenanceMode", "true");
        try
        {
            var connectionString = await fixture.CreateDatabaseAsync("game_config_etag");
            using var factory = new GameApiFactory(connectionString).ApplyEnvironment();
            using var other = factory.CreateClient();
            var second = await other.GetAsync(ApiRoutes.GameConfig);

            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.NotNull(second.Headers.ETag);
            Assert.NotEqual(firstEtag, second.Headers.ETag.Tag);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RemoteConfig__MaintenanceMode", null);
        }
    }

    [Fact]
    public async Task Invalid_minimum_version_prevents_startup()
    {
        Environment.SetEnvironmentVariable("RemoteConfig__MinimumClientVersion", "invalid");
        try
        {
            var connectionString = await fixture.CreateDatabaseAsync("game_config_invalid_version");
            using var factory = new GameApiFactory(connectionString).ApplyEnvironment();

            var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
            Assert.Contains("RemoteConfig:MinimumClientVersion", exception.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RemoteConfig__MinimumClientVersion", null);
        }
    }
}
