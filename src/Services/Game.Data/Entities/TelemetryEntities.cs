namespace Game.Data;

public sealed class TelemetryEvent
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public Guid SessionId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class TelemetryDailyAggregate
{
    public DateOnly Date { get; set; }
    public int RunCount { get; set; }
    public int KillCount { get; set; }
    public int BossDefeats { get; set; }
    public int LevelUps { get; set; }
    public int TotalDurationSeconds { get; set; }
    public int AverageRunDurationSeconds { get; set; }
    public int EventCount { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
