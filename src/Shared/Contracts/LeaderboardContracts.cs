namespace JabaSurvivol.Contracts;

public sealed class LeaderboardSubmissionRequest
{
    public Guid RunId { get; set; }
    public int HighestRound { get; set; }
    public int TotalKills { get; set; }
    public int DurationSeconds { get; set; }
    public string? ClientVersion { get; set; }
}

public sealed class LeaderboardSubmissionResponse
{
    public bool Accepted { get; set; }
    public Guid RunId { get; set; }
    public int HighestRound { get; set; }
    public int TotalKills { get; set; }
    public int DurationSeconds { get; set; }
    public DateTimeOffset AchievedAt { get; set; }
}

public sealed class LeaderboardEntry
{
    public int Rank { get; set; }
    public string Username { get; set; } = string.Empty;
    public int HighestRound { get; set; }
    public int TotalKills { get; set; }
    public int DurationSeconds { get; set; }
    public DateTimeOffset AchievedAt { get; set; }
}

public sealed class LeaderboardResponse
{
    public string Board { get; set; } = "highest-round";
    public IReadOnlyList<LeaderboardEntry> Entries { get; set; } = Array.Empty<LeaderboardEntry>();
}

public static class LeaderboardContract
{
    public const string BoardName = "highest-round";
    public const int DefaultLimit = 50;
    public const int MaximumLimit = 100;
    public const int MinimumHighestRound = 1;
    public const int MaximumHighestRound = 10;
    public const int MaximumKills = 100_000;
    public const int MaximumDurationSeconds = 86_400;
    public const int MaximumClientVersionLength = 64;
}
