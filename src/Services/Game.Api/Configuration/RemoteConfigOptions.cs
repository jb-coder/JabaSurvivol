using System.Globalization;
using System.Text.RegularExpressions;

namespace Game.Api.Configuration;

public sealed partial class RemoteConfigOptions
{
    public const string SectionName = "RemoteConfig";

    public int Version { get; set; } = 1;
    public string MinimumClientVersion { get; set; } = "0.5.0";
    public bool MaintenanceMode { get; set; }
    public bool CloudSavesEnabled { get; set; } = true;
    public bool LeaderboardsEnabled { get; set; } = true;
    public bool TelemetryEnabled { get; set; }
    public double TelemetrySampleRate { get; set; }

    public bool IsValid(out string error)
    {
        if (Version < 1)
        {
            error = "RemoteConfig:Version must be at least 1.";
            return false;
        }

        if (!ClientVersionPattern().IsMatch(MinimumClientVersion))
        {
            error = "RemoteConfig:MinimumClientVersion must use the major.minor.patch format.";
            return false;
        }

        if (TelemetrySampleRate < 0d || TelemetrySampleRate > 1d)
        {
            error = $"RemoteConfig:TelemetrySampleRate must be between 0 and 1, but was {TelemetrySampleRate.ToString(CultureInfo.InvariantCulture)}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    [GeneratedRegex("^[0-9]+\\.[0-9]+\\.[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ClientVersionPattern();
}
