using JabaSurvivol.Security;

namespace Bff;

public sealed class BffRateLimitMiddleware(RequestDelegate next, BffRateLimiter limiter)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var policy = ResolvePolicy(context.Request.Path);
        if (policy is null)
        {
            await next(context);
            return;
        }

        var partitionKey = ResolvePartitionKey(context, policy);
        if (limiter.TryAcquire(policy, partitionKey, out var retryAfterSeconds))
        {
            await next(context);
            return;
        }

        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ApiProblem.Create(
                StatusCodes.Status429TooManyRequests,
                "rate_limited",
                "Too many requests",
                "Retry after the indicated delay.")
            .ExecuteAsync(context);
    }

    private static string? ResolvePolicy(PathString path)
    {
        var value = path.Value ?? string.Empty;

        if (value.Equals("/api/v1/identity/me", StringComparison.OrdinalIgnoreCase))
        {
            return BffRateLimiter.AccountPolicy;
        }

        if (value.StartsWith("/api/v1/identity/", StringComparison.OrdinalIgnoreCase))
        {
            return BffRateLimiter.AuthenticationPolicy;
        }

        if (value.StartsWith("/api/v1/game/saves", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/api/v1/telemetry", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/api/v1/game/leaderboards/highest-round/runs", StringComparison.OrdinalIgnoreCase))
        {
            return BffRateLimiter.AccountPolicy;
        }

        if (value.Equals("/api/v1/game/config", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/api/v1/game/leaderboards/highest-round", StringComparison.OrdinalIgnoreCase))
        {
            return BffRateLimiter.IpPolicy;
        }

        return null;
    }

    private static string ResolvePartitionKey(HttpContext context, string policy)
    {
        if (policy == BffRateLimiter.AccountPolicy &&
            context.User.TryGetAccountId(out var accountId))
        {
            return accountId.ToString("N");
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
