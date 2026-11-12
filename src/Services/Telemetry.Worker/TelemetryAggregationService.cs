using System.Text.Json;
using Game.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Telemetry.Worker;

public sealed class TelemetryAggregationService(
    TelemetryDbContext database,
    TimeProvider timeProvider,
    IOptions<TelemetryWorkerOptions> options)
{
    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var configuration = options.Value;
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var lookbackDays = Math.Max(1, configuration.AggregationLookbackDays);
        var firstDate = today.AddDays(-(lookbackDays - 1));
        var firstInstant = firstDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endInstant = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        await using var transaction = await database.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var events = await database.TelemetryEvents
            .AsNoTracking()
            .Where(telemetryEvent =>
                telemetryEvent.OccurredAt >= firstInstant &&
                telemetryEvent.OccurredAt < endInstant)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var groups = events
            .GroupBy(telemetryEvent => DateOnly.FromDateTime(telemetryEvent.OccurredAt.UtcDateTime))
            .ToDictionary(group => group.Key, group => group.ToList());

        var dates = groups.Keys.ToList();
        var existing = await database.DailyAggregates
            .Where(aggregate => dates.Contains(aggregate.Date))
            .ToDictionaryAsync(aggregate => aggregate.Date, cancellationToken)
            .ConfigureAwait(false);

        foreach (var (date, dateEvents) in groups)
        {
            var aggregate = existing.TryGetValue(date, out var stored)
                ? stored
                : new TelemetryDailyAggregate { Date = date };

            aggregate.RunCount = dateEvents.Count(telemetryEvent =>
                telemetryEvent.EventType == "run_end");
            aggregate.KillCount = dateEvents.Count(telemetryEvent =>
                telemetryEvent.EventType == "zombie_kill");
            aggregate.BossDefeats = dateEvents.Count(telemetryEvent =>
                telemetryEvent.EventType == "boss_defeat");
            aggregate.LevelUps = dateEvents.Count(telemetryEvent =>
                telemetryEvent.EventType == "level_up");
            aggregate.EventCount = dateEvents.Count;
            aggregate.TotalDurationSeconds = dateEvents
                .Where(telemetryEvent => telemetryEvent.EventType == "run_end")
                .Sum(ReadRunDuration);
            aggregate.AverageRunDurationSeconds = aggregate.RunCount == 0
                ? 0
                : aggregate.TotalDurationSeconds / aggregate.RunCount;
            aggregate.UpdatedAt = now;

            if (!existing.ContainsKey(date))
            {
                database.DailyAggregates.Add(aggregate);
            }
        }

        await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var retentionDays = Math.Max(1, configuration.RetentionDays);
        var cutoff = now.AddDays(-retentionDays);
        await database.TelemetryEvents
            .Where(telemetryEvent => telemetryEvent.OccurredAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static int ReadRunDuration(TelemetryEvent telemetryEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(telemetryEvent.Payload);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                TryGetProperty(document.RootElement, "durationSeconds", out var duration) &&
                duration.ValueKind == JsonValueKind.Number &&
                duration.TryGetInt32(out var seconds))
            {
                return Math.Max(0, seconds);
            }
        }
        catch (JsonException)
        {
            // Invalid payloads are rejected at ingestion. Ignore a corrupt row
            // rather than failing the whole aggregation pass.
        }

        return 0;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
