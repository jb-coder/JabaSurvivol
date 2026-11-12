using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Game.Api.Telemetry;
using Game.Data;
using JabaSurvivol.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telemetry.Worker;

namespace JabaSurvivol.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class TelemetryEndpointTests(PostgresFixture fixture) : IAsyncLifetime
{
    private GameApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync("game_telemetry");
        _factory = new GameApiFactory(connectionString).ApplyEnvironment();
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Valid_batch_is_accepted_and_duplicates_are_ignored()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());
        var eventId = Guid.NewGuid();
        var batch = Envelope(
            RunEnd(eventId, 5, 10, 100),
            ZombieKill(Guid.NewGuid(), 5, "walker"));

        var first = await PostAsync(client, batch);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var accepted = await first.Content.ReadFromJsonAsync<TelemetryIngestionResponse>();
        Assert.NotNull(accepted);
        Assert.Equal(2, accepted.Accepted);
        Assert.Equal(0, accepted.Duplicates);

        var second = await PostAsync(client, batch);
        var duplicate = await second.Content.ReadFromJsonAsync<TelemetryIngestionResponse>();
        Assert.NotNull(duplicate);
        Assert.Equal(0, duplicate.Accepted);
        Assert.Equal(2, duplicate.Duplicates);

        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
        Assert.Equal(2, await database.TelemetryEvents.CountAsync());
    }

    [Fact]
    public async Task Batch_limits_and_allowlist_are_enforced()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());

        var empty = await PostAsync(client, "{\"events\":[]}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, empty.StatusCode);
        Assert.Equal("invalid_telemetry", await ReadCodeAsync(empty));

        var many = Envelope(Enumerable.Range(0, TelemetryContract.MaximumBatchSize + 1)
            .Select(index => RunEnd(Guid.NewGuid(), 5, 10, 100 + index))
            .ToArray());
        var oversizedBatch = await PostAsync(client, many);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, oversizedBatch.StatusCode);

        var unknownType = await PostAsync(client, Envelope(
            Event(Guid.NewGuid(), "unknown_event", Guid.NewGuid(), DateTimeOffset.UtcNow, "{}")));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownType.StatusCode);
        Assert.Equal("invalid_telemetry", await ReadCodeAsync(unknownType));

        var pii = await PostAsync(client, Envelope(
            Event(Guid.NewGuid(), "run_start", Guid.NewGuid(), DateTimeOffset.UtcNow,
                "{\"startingRound\":1,\"email\":\"player@example.com\"}")));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, pii.StatusCode);

        var oversizedPayload = await PostAsync(client, Envelope(
            RunStartWithPadding(new string('a', TelemetryContract.MaximumPayloadBytes + 1))));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, oversizedPayload.StatusCode);
    }

    [Fact]
    public async Task Timestamps_outside_the_window_are_rejected()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());
        var tooOld = await PostAsync(client, Envelope(
            RunEnd(Guid.NewGuid(), 5, 10, 100, DateTimeOffset.UtcNow.AddHours(-25))));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooOld.StatusCode);

        var tooFarInFuture = await PostAsync(client, Envelope(
            RunEnd(Guid.NewGuid(), 5, 10, 100, DateTimeOffset.UtcNow.AddMinutes(10))));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooFarInFuture.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_requests_return_problem_details()
    {
        using var client = _factory.CreateClient();

        var response = await PostAsync(client, Envelope(RunEnd(Guid.NewGuid(), 5, 10, 100)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("unauthorized", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Same_event_id_is_isolated_per_account()
    {
        var eventId = Guid.NewGuid();
        using var first = _factory.CreateClientFor(Guid.NewGuid());
        using var second = _factory.CreateClientFor(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.OK, (await PostAsync(first, Envelope(RunEnd(eventId, 5, 10, 100)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(second, Envelope(RunEnd(eventId, 6, 20, 200)))).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
        Assert.Equal(2, await database.TelemetryEvents.CountAsync());
    }

    [Fact]
    public async Task Oversized_batch_returns_413()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());
        var padding = new string('a', 800);
        var events = Enumerable.Range(0, TelemetryContract.MaximumBatchSize)
            .Select(index => Event(
                Guid.NewGuid(),
                "run_start",
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                $"{{\"startingRound\":1,\"padding\":\"{padding}{index}\"}}"))
            .ToArray();

        var response = await PostAsync(client, Envelope(events));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("payload_too_large", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Kestrel_body_limit_is_mapped_to_413_problem_details()
    {
        var read = await TelemetryBodyReader.ReadAsync(new ThrowingStream(), CancellationToken.None);

        var problem = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>(read.Error);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, problem.StatusCode);
        Assert.Equal("payload_too_large", problem.ProblemDetails.Extensions["code"]);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string json)
    {
        return await client.PostAsync(
            ApiRoutes.TelemetryEvents,
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private static async Task<string> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("code").GetString() ?? string.Empty;
    }

    private static string Envelope(params string[] events)
    {
        return $"{{\"events\":[{string.Join(",", events)}]}}";
    }

    private static string RunEnd(
        Guid eventId,
        int highestRound,
        int totalKills,
        int durationSeconds,
        DateTimeOffset? occurredAt = null)
    {
        var payload = $"{{\"highestRound\":{highestRound},\"totalKills\":{totalKills},\"durationSeconds\":{durationSeconds}}}";
        return Event(eventId, "run_end", Guid.NewGuid(), occurredAt ?? DateTimeOffset.UtcNow, payload);
    }

    private static string ZombieKill(Guid eventId, int round, string archetype)
    {
        return Event(eventId, "zombie_kill", Guid.NewGuid(), DateTimeOffset.UtcNow,
            $"{{\"round\":{round},\"archetype\":\"{archetype}\"}}");
    }

    private static string RunStartWithPadding(string padding)
    {
        return Event(Guid.NewGuid(), "run_start", Guid.NewGuid(), DateTimeOffset.UtcNow,
            $"{{\"startingRound\":1,\"padding\":\"{padding}\"}}");
    }

    private static string Event(
        Guid eventId,
        string eventType,
        Guid sessionId,
        DateTimeOffset occurredAt,
        string payload)
    {
        return $"{{\"eventId\":\"{eventId}\",\"eventType\":\"{eventType}\",\"occurredAt\":\"{occurredAt:O}\",\"sessionId\":\"{sessionId}\",\"payload\":{payload}}}";
    }

    private sealed class ThrowingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new BadHttpRequestException("The request body is too large.", StatusCodes.Status413PayloadTooLarge);
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            throw new BadHttpRequestException("The request body is too large.", StatusCodes.Status413PayloadTooLarge);
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            throw new BadHttpRequestException("The request body is too large.", StatusCodes.Status413PayloadTooLarge);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

[Collection(PostgresCollection.Name)]
public sealed class TelemetryAggregationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private GameApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync("game_telemetry_agg");
        _factory = new GameApiFactory(connectionString).ApplyEnvironment();
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Aggregation_is_idempotent_and_retention_removes_old_events()
    {
        var now = DateTimeOffset.UtcNow;
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

        database.TelemetryEvents.AddRange(
            NewEvent("run_end", now, "{\"highestRound\":5,\"totalKills\":10,\"durationSeconds\":100}"),
            NewEvent("run_end", now, "{\"highestRound\":6,\"totalKills\":20,\"durationSeconds\":200}"),
            NewEvent("zombie_kill", now, "{\"round\":5,\"archetype\":\"walker\"}"),
            NewEvent("boss_defeat", now, "{\"round\":10}"),
            NewEvent("level_up", now, "{\"level\":3}"),
            NewEvent("run_start", now.AddDays(-31), "{\"startingRound\":1}"));
        await database.SaveChangesAsync();

        var service = new TelemetryAggregationService(
            database,
            TimeProvider.System,
            Options.Create(new TelemetryWorkerOptions
            {
                RetentionDays = 30,
                AggregationLookbackDays = 2
            }));

        await service.RunOnceAsync(CancellationToken.None);
        await service.RunOnceAsync(CancellationToken.None);

        database.ChangeTracker.Clear();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var aggregate = await database.DailyAggregates.SingleAsync(item => item.Date == today);
        Assert.Equal(2, aggregate.RunCount);
        Assert.Equal(1, aggregate.KillCount);
        Assert.Equal(1, aggregate.BossDefeats);
        Assert.Equal(1, aggregate.LevelUps);
        Assert.Equal(5, aggregate.EventCount);
        Assert.Equal(300, aggregate.TotalDurationSeconds);
        Assert.Equal(150, aggregate.AverageRunDurationSeconds);

        Assert.Equal(5, await database.TelemetryEvents.CountAsync());
        Assert.DoesNotContain(
            await database.TelemetryEvents.Select(item => item.OccurredAt).ToListAsync(),
            occurredAt => occurredAt < DateTimeOffset.UtcNow.AddDays(-30));
    }

    private static TelemetryEvent NewEvent(string eventType, DateTimeOffset occurredAt, string payload)
    {
        return new TelemetryEvent
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            EventType = eventType,
            OccurredAt = occurredAt,
            SessionId = Guid.NewGuid(),
            Payload = payload,
            ReceivedAt = DateTimeOffset.UtcNow
        };
    }
}
