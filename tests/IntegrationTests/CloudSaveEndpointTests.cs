using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JabaSurvivol.Contracts;
using JabaSurvivol.Core;

namespace JabaSurvivol.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class CloudSaveEndpointTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ProfileOptions = new() { IncludeFields = true };

    private GameApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync("game_save");
        _factory = new GameApiFactory(connectionString).ApplyEnvironment();
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task First_save_requires_revision_zero_and_returns_etag()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());

        var response = await client.PutAsync(
            ApiRoutes.CloudSavePrimary,
            CreatePayload(0, ProfileJson(3)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var save = await response.Content.ReadFromJsonAsync<SaveResponse>();
        Assert.NotNull(save);
        Assert.Equal(1, save.Revision);
        Assert.Equal(3, save.Document.GetProperty("HighestRound").GetInt32());
        Assert.Equal("\"rev-1\"", save.ETag);
        Assert.Equal("\"rev-1\"", response.Headers.ETag?.Tag);

        var get = await client.GetAsync(ApiRoutes.CloudSavePrimary);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("\"rev-1\"", get.Headers.ETag?.Tag);

        using var conditional = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.CloudSavePrimary);
        conditional.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"rev-1\""));
        var notModified = await client.SendAsync(conditional);
        Assert.Equal(HttpStatusCode.NotModified, notModified.StatusCode);
    }

    [Fact]
    public async Task Save_update_increments_revision_atomically()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());
        await client.PutAsync(ApiRoutes.CloudSavePrimary, CreatePayload(0, ProfileJson(2)));

        var update = await client.PutAsync(
            ApiRoutes.CloudSavePrimary,
            CreatePayload(1, ProfileJson(4)));

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var save = await update.Content.ReadFromJsonAsync<SaveResponse>();
        Assert.NotNull(save);
        Assert.Equal(2, save.Revision);
        Assert.Equal(4, save.Document.GetProperty("HighestRound").GetInt32());
    }

    [Fact]
    public async Task Stale_revision_returns_conflict_without_overwriting()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());
        await client.PutAsync(ApiRoutes.CloudSavePrimary, CreatePayload(0, ProfileJson(2)));
        await client.PutAsync(ApiRoutes.CloudSavePrimary, CreatePayload(1, ProfileJson(5)));

        var stale = await client.PutAsync(
            ApiRoutes.CloudSavePrimary,
            CreatePayload(1, ProfileJson(1)));

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var problem = JsonDocument.Parse(await stale.Content.ReadAsStringAsync());
        Assert.Equal("revision_conflict", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal(2, problem.RootElement.GetProperty("currentRevision").GetInt64());

        var get = await client.GetFromJsonAsync<SaveResponse>(ApiRoutes.CloudSavePrimary);
        Assert.NotNull(get);
        Assert.Equal(2, get.Revision);
        Assert.Equal(5, get.Document.GetProperty("HighestRound").GetInt32());
    }

    [Fact]
    public async Task First_save_with_nonzero_revision_returns_conflict()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());

        var response = await client.PutAsync(
            ApiRoutes.CloudSavePrimary,
            CreatePayload(1, ProfileJson(2)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, problem.RootElement.GetProperty("currentRevision").GetInt64());
    }

    [Fact]
    public async Task Oversized_request_returns_413()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());
        var padding = new string('a', SaveContract.MaximumBodyBytes + 1);
        var payload = CreatePayload(0, $"{{\"schemaVersion\":1,\"supplies\":1,\"xp\":0,\"level\":1,\"skillPoints\":0,\"highestRound\":1,\"padding\":\"{padding}\"}}");

        var response = await client.PutAsync(ApiRoutes.CloudSavePrimary, payload);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_json_returns_422()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());

        var response = await client.PutAsync(
            ApiRoutes.CloudSavePrimary,
            new StringContent("{ not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("invalid_save", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Unsupported_schema_returns_422()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());
        var document = ProfileJson(2).Replace("\"SchemaVersion\":1", "\"SchemaVersion\":2", StringComparison.Ordinal);

        var response = await client.PutAsync(ApiRoutes.CloudSavePrimary, CreatePayload(0, document));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("invalid_save", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Missing_required_profile_field_returns_422()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());
        const string document = "{\"schemaVersion\":1,\"supplies\":0,\"xp\":0,\"level\":1,\"highestRound\":1}";

        var response = await client.PutAsync(ApiRoutes.CloudSavePrimary, CreatePayload(0, document));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("invalid_save", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Unauthenticated_requests_return_problem_details()
    {
        using var client = _factory.CreateClient();

        var get = await client.GetAsync(ApiRoutes.CloudSavePrimary);
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal("unauthorized", await ReadCodeAsync(get));

        var put = await client.PutAsync(ApiRoutes.CloudSavePrimary, CreatePayload(0, ProfileJson(1)));
        Assert.Equal(HttpStatusCode.Unauthorized, put.StatusCode);
    }

    [Fact]
    public async Task Saves_are_isolated_between_accounts()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        using var firstClient = _factory.CreateClientFor(first);
        using var secondClient = _factory.CreateClientFor(second);

        await firstClient.PutAsync(ApiRoutes.CloudSavePrimary, CreatePayload(0, ProfileJson(6)));

        var secondGet = await secondClient.GetAsync(ApiRoutes.CloudSavePrimary);
        Assert.Equal(HttpStatusCode.NotFound, secondGet.StatusCode);

        var secondPut = await secondClient.PutAsync(
            ApiRoutes.CloudSavePrimary,
            CreatePayload(0, ProfileJson(2)));
        Assert.Equal(HttpStatusCode.OK, secondPut.StatusCode);
        var secondSave = await secondPut.Content.ReadFromJsonAsync<SaveResponse>();
        Assert.NotNull(secondSave);
        Assert.Equal(1, secondSave.Revision);

        var firstGet = await firstClient.GetFromJsonAsync<SaveResponse>(ApiRoutes.CloudSavePrimary);
        Assert.NotNull(firstGet);
        Assert.Equal(6, firstGet.Document.GetProperty("HighestRound").GetInt32());
    }

    private static string ProfileJson(int highestRound)
    {
        var profile = new Profile
        {
            Supplies = 100,
            Xp = 0,
            Level = 1,
            SkillPoints = 0,
            HighestRound = highestRound
        };

        return JsonSerializer.Serialize(profile, ProfileOptions);
    }

    private static StringContent CreatePayload(long baseRevision, string document)
    {
        var json = $"{{\"baseRevision\":{baseRevision},\"document\":{document}}}";
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task<string> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("code").GetString() ?? string.Empty;
    }
}
