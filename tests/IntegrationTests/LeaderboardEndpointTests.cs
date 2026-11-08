using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Api.Leaderboards;
using Game.Data;
using JabaSurvivol.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace JabaSurvivol.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class LeaderboardEndpointTests(PostgresFixture fixture) : IAsyncLifetime
{
    private GameApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync("game_leaderboard");
        _factory = new GameApiFactory(connectionString).ApplyEnvironment();
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Submission_and_query_return_ranked_entry()
    {
        var accountId = Guid.NewGuid();
        using var client = _factory.CreateClientFor(accountId, "TopSurvivor");

        var submit = await SubmitAsync(client, Guid.NewGuid(), 7, 210, 900, "0.5.0");
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        var board = await client.GetFromJsonAsync<LeaderboardResponse>(ApiRoutes.LeaderboardTop);
        Assert.NotNull(board);
        var entry = Assert.Single(board.Entries);
        Assert.Equal(1, entry.Rank);
        Assert.Equal("TopSurvivor", entry.Username);
        Assert.Equal(7, entry.HighestRound);
        Assert.Equal(210, entry.TotalKills);
        Assert.Equal(900, entry.DurationSeconds);
    }

    [Fact]
    public async Task Duplicate_run_submission_is_idempotent_and_keeps_original_values()
    {
        var accountId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        using var client = _factory.CreateClientFor(accountId, "Idempotent");
        await SubmitAsync(client, runId, 8, 300, 1_000, "0.5.0");

        var duplicate = await SubmitAsync(client, runId, 2, 10, 10, "0.5.1");
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        var accepted = await duplicate.Content.ReadFromJsonAsync<LeaderboardSubmissionResponse>();
        Assert.NotNull(accepted);
        Assert.Equal(8, accepted.HighestRound);
        Assert.Equal(300, accepted.TotalKills);

        var board = await client.GetFromJsonAsync<LeaderboardResponse>(ApiRoutes.LeaderboardTop);
        Assert.NotNull(board);
        var entry = Assert.Single(board.Entries);
        Assert.Equal(8, entry.HighestRound);
    }

    [Fact]
    public async Task Board_returns_only_the_best_run_per_account()
    {
        var accountId = Guid.NewGuid();
        using var client = _factory.CreateClientFor(accountId, "BestRun");

        await SubmitAsync(client, Guid.NewGuid(), 3, 50, 400, "0.5.0");
        await SubmitAsync(client, Guid.NewGuid(), 8, 250, 1_100, "0.5.0");

        var board = await client.GetFromJsonAsync<LeaderboardResponse>(ApiRoutes.LeaderboardTop);
        Assert.NotNull(board);
        var entry = Assert.Single(board.Entries);
        Assert.Equal(8, entry.HighestRound);
        Assert.Equal(250, entry.TotalKills);
    }

    [Fact]
    public async Task Tiebreaks_order_by_round_kills_duration_and_time()
    {
        using var alpha = _factory.CreateClientFor(Guid.NewGuid(), "Alpha");
        using var bravo = _factory.CreateClientFor(Guid.NewGuid(), "Bravo");
        using var charlie = _factory.CreateClientFor(Guid.NewGuid(), "Charlie");

        await SubmitAsync(alpha, Guid.NewGuid(), 5, 100, 100, "0.5.0");
        await Task.Delay(10);
        await SubmitAsync(bravo, Guid.NewGuid(), 5, 100, 50, "0.5.0");
        await Task.Delay(10);
        await SubmitAsync(charlie, Guid.NewGuid(), 5, 100, 50, "0.5.0");

        var board = await alpha.GetFromJsonAsync<LeaderboardResponse>(ApiRoutes.LeaderboardTop);
        Assert.NotNull(board);
        Assert.Equal(["Bravo", "Charlie", "Alpha"], board.Entries.Select(entry => entry.Username));
        Assert.Equal([1, 2, 3], board.Entries.Select(entry => entry.Rank));
    }

    [Fact]
    public async Task Ordering_uses_round_then_kills_across_accounts()
    {
        using var alpha = _factory.CreateClientFor(Guid.NewGuid(), "Alpha");
        using var bravo = _factory.CreateClientFor(Guid.NewGuid(), "Bravo");
        using var charlie = _factory.CreateClientFor(Guid.NewGuid(), "Charlie");

        await SubmitAsync(alpha, Guid.NewGuid(), 8, 10, 100, "0.5.0");
        await SubmitAsync(bravo, Guid.NewGuid(), 7, 500, 100, "0.5.0");
        await SubmitAsync(charlie, Guid.NewGuid(), 7, 600, 100, "0.5.0");

        var board = await alpha.GetFromJsonAsync<LeaderboardResponse>(ApiRoutes.LeaderboardTop);

        Assert.NotNull(board);
        Assert.Equal(["Alpha", "Charlie", "Bravo"], board.Entries.Select(entry => entry.Username));
        Assert.Equal([8, 7, 7], board.Entries.Select(entry => entry.HighestRound));
        Assert.Equal([10, 600, 500], board.Entries.Select(entry => entry.TotalKills));
        Assert.Equal([1, 2, 3], board.Entries.Select(entry => entry.Rank));
    }

    [Fact]
    public async Task Limit_is_applied_and_capped_through_the_endpoint()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var now = DateTimeOffset.UtcNow;
            for (var index = 0; index < LeaderboardContract.MaximumLimit + 1; index++)
            {
                database.LeaderboardRuns.Add(new LeaderboardRun
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    RunId = Guid.NewGuid(),
                    Username = $"Player{index:000}",
                    HighestRound = (index % LeaderboardContract.MaximumHighestRound) + 1,
                    TotalKills = index,
                    DurationSeconds = 60 + index,
                    ClientVersion = "0.5.0",
                    AchievedAt = now.AddSeconds(index)
                });
            }

            await database.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        var limited = await client.GetFromJsonAsync<LeaderboardResponse>($"{ApiRoutes.LeaderboardTop}?limit=2");
        Assert.NotNull(limited);
        Assert.Equal(2, limited.Entries.Count);
        Assert.Equal([1, 2], limited.Entries.Select(entry => entry.Rank));

        var capped = await client.GetFromJsonAsync<LeaderboardResponse>($"{ApiRoutes.LeaderboardTop}?limit=500");
        Assert.NotNull(capped);
        Assert.Equal(LeaderboardContract.MaximumLimit, capped.Entries.Count);
        Assert.Equal(LeaderboardContract.MaximumLimit, capped.Entries[^1].Rank);
    }

    [Fact]
    public async Task Bounds_are_validated()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());

        var round = await SubmitAsync(client, Guid.NewGuid(), 11, 0, 10, "0.5.0");
        Assert.Equal(HttpStatusCode.BadRequest, round.StatusCode);
        Assert.Equal("invalid_run", await ReadCodeAsync(round));

        var duration = await SubmitAsync(client, Guid.NewGuid(), 5, 0, 0, "0.5.0");
        Assert.Equal(HttpStatusCode.BadRequest, duration.StatusCode);

        var version = await SubmitAsync(client, Guid.NewGuid(), 5, 0, 10, string.Empty);
        Assert.Equal(HttpStatusCode.BadRequest, version.StatusCode);
    }

    [Fact]
    public async Task Empty_board_returns_an_empty_list()
    {
        using var client = _factory.CreateClient();

        var board = await client.GetFromJsonAsync<LeaderboardResponse>(ApiRoutes.LeaderboardTop);

        Assert.NotNull(board);
        Assert.Empty(board.Entries);
    }

    [Fact]
    public void Limit_is_clamped_to_the_documented_range()
    {
        Assert.Equal(LeaderboardContract.DefaultLimit, LeaderboardEndpoints.ClampLimit(null));
        Assert.Equal(LeaderboardContract.DefaultLimit, LeaderboardEndpoints.ClampLimit(0));
        Assert.Equal(25, LeaderboardEndpoints.ClampLimit(25));
        Assert.Equal(LeaderboardContract.MaximumLimit, LeaderboardEndpoints.ClampLimit(500));
    }

    [Fact]
    public async Task Unauthenticated_submission_returns_problem_details()
    {
        using var client = _factory.CreateClient();

        var response = await SubmitAsync(client, Guid.NewGuid(), 5, 10, 100, "0.5.0");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("unauthorized", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Same_run_id_is_isolated_per_account()
    {
        var runId = Guid.NewGuid();
        using var first = _factory.CreateClientFor(Guid.NewGuid(), "First");
        using var second = _factory.CreateClientFor(Guid.NewGuid(), "Second");

        Assert.Equal(HttpStatusCode.OK, (await SubmitAsync(first, runId, 6, 100, 600, "0.5.0")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SubmitAsync(second, runId, 4, 80, 500, "0.5.0")).StatusCode);

        var board = await first.GetFromJsonAsync<LeaderboardResponse>(ApiRoutes.LeaderboardTop);
        Assert.NotNull(board);
        Assert.Equal(2, board.Entries.Count);
    }

    [Fact]
    public async Task Public_board_excludes_private_account_data()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid(), "PublicName");
        await SubmitAsync(client, Guid.NewGuid(), 5, 10, 100, "0.5.0");

        var json = await client.GetStringAsync(ApiRoutes.LeaderboardTop);

        Assert.DoesNotContain("accountId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("runId", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PublicName", json, StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> SubmitAsync(
        HttpClient client,
        Guid runId,
        int highestRound,
        int totalKills,
        int durationSeconds,
        string clientVersion)
    {
        return await client.PostAsJsonAsync(
            ApiRoutes.LeaderboardSubmit,
            new LeaderboardSubmissionRequest
            {
                RunId = runId,
                HighestRound = highestRound,
                TotalKills = totalKills,
                DurationSeconds = durationSeconds,
                ClientVersion = clientVersion
            });
    }

    private static async Task<string> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("code").GetString() ?? string.Empty;
    }
}

[Collection(PostgresCollection.Name)]
public sealed class LeaderboardRateLimitTests(PostgresFixture fixture) : IAsyncLifetime
{
    private GameApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync("game_leaderboard_rate");
        _factory = new GameApiFactory(connectionString, leaderboardPermitLimit: 2).ApplyEnvironment();
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Submission_rate_limit_returns_429_with_retry_after()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());
        var request = new LeaderboardSubmissionRequest
        {
            RunId = Guid.NewGuid(),
            HighestRound = 2,
            TotalKills = 10,
            DurationSeconds = 60,
            ClientVersion = "0.5.0"
        };

        await client.PostAsJsonAsync(ApiRoutes.LeaderboardSubmit, request);
        request.RunId = Guid.NewGuid();
        await client.PostAsJsonAsync(ApiRoutes.LeaderboardSubmit, request);
        request.RunId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync(ApiRoutes.LeaderboardSubmit, request);

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.True(response.Headers.RetryAfter is not null || response.Headers.Contains("Retry-After"));
    }
}
