using System.Text;
using JabaSurvivol.Contracts;
using JabaSurvivol.Security;

namespace Game.Api.Telemetry;

public readonly record struct TelemetryBodyReadResult(string? Body, IResult? Error);

public static class TelemetryBodyReader
{
    public static async Task<TelemetryBodyReadResult> ReadAsync(
        Stream body,
        CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(
                body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 8192,
                leaveOpen: true);

            var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            if (Encoding.UTF8.GetByteCount(text) > TelemetryContract.MaximumBodyBytes)
            {
                return new TelemetryBodyReadResult(null, PayloadTooLarge());
            }

            return new TelemetryBodyReadResult(text, null);
        }
        catch (BadHttpRequestException)
        {
            return new TelemetryBodyReadResult(null, PayloadTooLarge());
        }
    }

    public static IResult PayloadTooLarge()
    {
        return ApiProblem.Create(
            StatusCodes.Status413PayloadTooLarge,
            "payload_too_large",
            "Telemetry batch is too large",
            $"The maximum request size is {TelemetryContract.MaximumBodyBytes} bytes.");
    }
}
