using JabaSurvivol.Security;
using Yarp.ReverseProxy.Forwarder;

namespace Bff;

public sealed class ProxyErrorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.HasStarted)
        {
            return;
        }

        var feature = context.Features.Get<IForwarderErrorFeature>();
        if (feature is null)
        {
            return;
        }

        var timedOut = feature.Error is ForwarderError.RequestTimedOut or ForwarderError.RequestCanceled;
        var statusCode = timedOut
            ? StatusCodes.Status504GatewayTimeout
            : StatusCodes.Status502BadGateway;

        context.Response.StatusCode = statusCode;
        await ApiProblem.Create(
                statusCode,
                timedOut ? "upstream_timeout" : "upstream_unavailable",
                timedOut ? "Upstream timed out" : "Upstream unavailable",
                "The request could not be completed. Retry later.")
            .ExecuteAsync(context);
    }
}
