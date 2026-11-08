namespace Game.Data;

public sealed class CloudSave
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Slot { get; set; } = "primary";
    public long Revision { get; set; }
    public int SchemaVersion { get; set; }
    public string Document { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
