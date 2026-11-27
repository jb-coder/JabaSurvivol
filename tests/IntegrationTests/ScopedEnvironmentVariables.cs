namespace JabaSurvivol.IntegrationTests;

/// <summary>
/// Applies process-wide environment variable overrides for the duration of a
/// single test and restores the exact previous values when disposed. The gate
/// is held for the whole scope so overrides from one test cannot be observed by
/// another test that runs on a different thread.
/// </summary>
public sealed class ScopedEnvironmentVariables : IDisposable
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly Dictionary<string, string?> _previousValues;
    private bool _disposed;

    private ScopedEnvironmentVariables(Dictionary<string, string?> previousValues)
    {
        _previousValues = previousValues;
    }

    public static async Task<ScopedEnvironmentVariables> ApplyAsync(
        params (string Name, string? Value)[] overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);
        await Gate.WaitAsync().ConfigureAwait(false);

        var previousValues = new Dictionary<string, string?>(StringComparer.Ordinal);
        try
        {
            foreach (var (name, value) in overrides)
            {
                previousValues.TryAdd(name, Environment.GetEnvironmentVariable(name));
                Environment.SetEnvironmentVariable(name, value);
            }

            return new ScopedEnvironmentVariables(previousValues);
        }
        catch
        {
            foreach (var (name, value) in previousValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }

            Gate.Release();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            foreach (var (name, value) in _previousValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
        finally
        {
            Gate.Release();
        }
    }
}
