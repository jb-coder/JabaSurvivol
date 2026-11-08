using Microsoft.Extensions.Options;

namespace Telemetry.Worker;

public sealed class TelemetryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<TelemetryWorkerOptions> options,
    ILogger<TelemetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(10, options.Value.WorkerIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TelemetryAggregationService>();
                await service.RunOnceAsync(stoppingToken).ConfigureAwait(false);
                logger.LogInformation("Telemetry aggregation completed.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Telemetry aggregation failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
