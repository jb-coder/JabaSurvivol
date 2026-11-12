using System.Text;

namespace Bff;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Sanitize(context.Request.Headers[HeaderName].ToString());

        context.Items[HeaderName] = correlationId;
        context.Request.Headers[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(Math.Min(value.Length, 64));
        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')
            {
                builder.Append(character);
            }

            if (builder.Length == 64)
            {
                break;
            }
        }

        return builder.Length > 0 ? builder.ToString() : Guid.NewGuid().ToString("N");
    }
}
