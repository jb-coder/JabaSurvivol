using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using JabaSurvivol.Contracts;
using Microsoft.Extensions.Options;

namespace Game.Api.Configuration;

public static class RemoteConfigEndpoint
{
    public static IEndpointRouteBuilder MapRemoteConfigEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(ApiRoutes.GameConfig, GetConfig)
            .AllowAnonymous()
            .WithTags("Configuration")
            .WithName("GetRemoteConfig");

        return endpoints;
    }

    private static IResult GetConfig(
        IOptions<RemoteConfigOptions> options,
        HttpContext httpContext)
    {
        var value = options.Value;
        var response = new RemoteConfigResponse
        {
            Version = value.Version,
            MinimumClientVersion = value.MinimumClientVersion,
            MaintenanceMode = value.MaintenanceMode,
            CloudSavesEnabled = value.CloudSavesEnabled,
            LeaderboardsEnabled = value.LeaderboardsEnabled,
            TelemetryEnabled = value.TelemetryEnabled,
            TelemetrySampleRate = value.TelemetrySampleRate
        };

        var etag = CreateEtag(value);
        httpContext.Response.Headers.ETag = etag;
        httpContext.Response.Headers.CacheControl = "public, max-age=60";

        var ifNoneMatch = httpContext.Request.Headers.IfNoneMatch.ToString();
        if (ifNoneMatch.Contains(etag, StringComparison.Ordinal) ||
            ifNoneMatch.Contains("*", StringComparison.Ordinal))
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        return Results.Ok(response);
    }

    private static string CreateEtag(RemoteConfigOptions value)
    {
        var canonical = string.Join(
            "|",
            value.Version.ToString(CultureInfo.InvariantCulture),
            value.MinimumClientVersion,
            value.MaintenanceMode,
            value.CloudSavesEnabled,
            value.LeaderboardsEnabled,
            value.TelemetryEnabled,
            value.TelemetrySampleRate.ToString("R", CultureInfo.InvariantCulture));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return $"\"{Convert.ToHexString(hash)[..16].ToLowerInvariant()}\"";
    }
}
