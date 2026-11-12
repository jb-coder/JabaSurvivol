using System.Text.Json;

namespace JabaSurvivol.Contracts;

public sealed class SaveWriteRequest
{
    public long BaseRevision { get; set; }
    public JsonElement Document { get; set; }
}

public sealed class SaveResponse
{
    public string Slot { get; set; } = "primary";
    public long Revision { get; set; }
    public int SchemaVersion { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string ETag { get; set; } = string.Empty;
    public JsonElement Document { get; set; }
}

public static class SaveContract
{
    public const string PrimarySlot = "primary";
    public const int MaximumBodyBytes = 1024 * 1024;
    public const int SupportedSchemaVersion = 1;
}
