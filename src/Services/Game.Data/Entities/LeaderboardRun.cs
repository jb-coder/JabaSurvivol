namespace Game.Data;

public sealed class LeaderboardRun
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid RunId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int HighestRound { get; set; }
    public int TotalKills { get; set; }
    public int DurationSeconds { get; set; }
    public string ClientVersion { get; set; } = string.Empty;
    public DateTimeOffset AchievedAt { get; set; }
}
