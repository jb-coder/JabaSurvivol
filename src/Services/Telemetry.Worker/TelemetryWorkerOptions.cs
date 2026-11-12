namespace Telemetry.Worker;

public sealed class TelemetryWorkerOptions
{
    public const string SectionName = "Telemetry";

    public int WorkerIntervalSeconds { get; set; } = 60;
    public int RetentionDays { get; set; } = 30;
    public int AggregationLookbackDays { get; set; } = 2;
}
