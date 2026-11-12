using System.Text.Json;

namespace JabaSurvivol.Contracts;

public sealed class TelemetryEnvelope
{
    public List<TelemetryEventRequest>? Events { get; set; }
}

public sealed class TelemetryEventRequest
{
    public Guid EventId { get; set; }
    public string? EventType { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid SessionId { get; set; }
    public JsonElement Payload { get; set; }
}

public sealed class TelemetryIngestionResponse
{
    public int Accepted { get; set; }
    public int Duplicates { get; set; }
}

public static class TelemetryContract
{
    public const int MaximumBatchSize = 100;
    public const int MaximumBodyBytes = 64 * 1024;
    public const int MaximumPayloadBytes = 4 * 1024;
    public const int MaximumEventTypeLength = 64;
    public static readonly TimeSpan MaximumAge = TimeSpan.FromHours(24);
    public static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromMinutes(5);

    public const string RunStart = "run_start";
    public const string RunEnd = "run_end";
    public const string RoundClear = "round_clear";
    public const string ZombieKill = "zombie_kill";
    public const string BossDefeat = "boss_defeat";
    public const string LevelUp = "level_up";
    public const string RewardSelection = "reward_selection";
    public const string WeaponSwitch = "weapon_switch";

    public static ISet<string> EventTypes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        RunStart,
        RunEnd,
        RoundClear,
        ZombieKill,
        BossDefeat,
        LevelUp,
        RewardSelection,
        WeaponSwitch
    };
}
