using System.Text;
using JabaSurvivol.Contracts;
using JabaSurvivol.Security;

namespace Game.Api.Saves;

public readonly record struct SaveBodyReadResult(string? Body, IResult? Error);

public static class SaveBodyReader
{
    public static async Task<SaveBodyReadResult> ReadAsync(Stream body, CancellationToken cancellationToken)
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
            if (Encoding.UTF8.GetByteCount(text) > SaveContract.MaximumBodyBytes)
            {
                return new SaveBodyReadResult(null, PayloadTooLarge());
            }

            return new SaveBodyReadResult(text, null);
        }
        catch (BadHttpRequestException)
        {
            return new SaveBodyReadResult(null, PayloadTooLarge());
        }
    }

    public static IResult PayloadTooLarge()
    {
        return ApiProblem.Create(
            StatusCodes.Status413PayloadTooLarge,
            "payload_too_large",
            "Cloud save is too large",
            $"The maximum request size is {SaveContract.MaximumBodyBytes} bytes.");
    }
}
