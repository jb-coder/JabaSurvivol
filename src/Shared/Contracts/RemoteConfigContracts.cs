namespace JabaSurvivol.Contracts;

public sealed class RemoteConfigResponse
{
    public int Version { get; set; }
    public string MinimumClientVersion { get; set; } = string.Empty;
    public bool MaintenanceMode { get; set; }
    public bool CloudSavesEnabled { get; set; }
    public bool LeaderboardsEnabled { get; set; }
    public bool TelemetryEnabled { get; set; }
    public double TelemetrySampleRate { get; set; }
}
