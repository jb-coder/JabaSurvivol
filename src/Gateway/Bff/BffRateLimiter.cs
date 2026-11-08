using System.Collections.Concurrent;

namespace Bff;

public sealed class BffRateLimiter(TimeProvider timeProvider, IConfiguration configuration)
{
    public const string AuthenticationPolicy = "auth";
    public const string AccountPolicy = "account";
    public const string IpPolicy = "ip";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, WindowState> _windows = new(StringComparer.Ordinal);

    private readonly int _authLimit = configuration.GetValue("RateLimiting:AuthPermitLimit", 10);
    private readonly int _accountLimit = configuration.GetValue("RateLimiting:AccountPermitLimit", 60);
    private readonly int _ipLimit = configuration.GetValue("RateLimiting:IpPermitLimit", 120);

    public bool TryAcquire(string policy, string partitionKey, out int retryAfterSeconds)
    {
        var limit = policy switch
        {
            AuthenticationPolicy => _authLimit,
            AccountPolicy => _accountLimit,
            _ => _ipLimit
        };

        var now = timeProvider.GetUtcNow();
        if (_windows.Count > 10_000)
        {
            foreach (var entry in _windows)
            {
                if (entry.Value.ResetAt <= now)
                {
                    _windows.TryRemove(entry.Key, out _);
                }
            }
        }

        var compositeKey = $"{policy}|{partitionKey}";
        var window = _windows.AddOrUpdate(
            compositeKey,
            _ => new WindowState(now.Add(Window), 1),
            (_, existing) => existing.ResetAt <= now
                ? new WindowState(now.Add(Window), 1)
                : existing with { Count = existing.Count + 1 });

        if (window.Count <= Math.Max(1, limit))
        {
            retryAfterSeconds = 0;
            return true;
        }

        retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((window.ResetAt - now).TotalSeconds));
        return false;
    }

    private readonly record struct WindowState(DateTimeOffset ResetAt, int Count);
}
