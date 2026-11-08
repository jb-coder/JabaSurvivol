using System.Text.Json;
using Game.Data;
using JabaSurvivol.Contracts;
using JabaSurvivol.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game.Api.Telemetry;

public static class TelemetryEndpoints
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IEndpointRouteBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(ApiRoutes.TelemetryEvents, IngestAsync)
            .RequireAuthorization()
            .WithTags("Telemetry")
            .WithMetadata(new RequestSizeLimitAttribute(TelemetryContract.MaximumBodyBytes))
            .WithName("IngestTelemetryEvents");

        return endpoints;
    }

    private static async Task<IResult> IngestAsync(
        HttpContext httpContext,
        TelemetryDbContext database,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetAccountId(out var accountId))
        {
            return ApiProblem.Unauthorized();
        }

        var readResult = await TelemetryBodyReader
            .ReadAsync(httpContext.Request.Body, cancellationToken)
            .ConfigureAwait(false);

        if (readResult.Error is not null)
        {
            return readResult.Error;
        }

        TelemetryEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<TelemetryEnvelope>(
                readResult.Body!,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return InvalidTelemetry("The request body is not valid JSON.");
        }

        var now = timeProvider.GetUtcNow();
        if (!TelemetryValidator.TryValidateEnvelope(envelope, now, out var validationError))
        {
            return InvalidTelemetry(validationError);
        }

        var accepted = 0;
        var duplicates = 0;

        foreach (var telemetryEvent in envelope!.Events!)
        {
            var affected = await database.Database
                .ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO telemetry."TelemetryEvents"
                        ("Id", "AccountId", "EventId", "EventType", "OccurredAt", "SessionId", "Payload", "ReceivedAt")
                    VALUES
                        ({Guid.NewGuid()}, {accountId}, {telemetryEvent.EventId}, {telemetryEvent.EventType!}, {telemetryEvent.OccurredAt}, {telemetryEvent.SessionId}, {telemetryEvent.Payload.GetRawText()}, {now})
                    ON CONFLICT ("AccountId", "EventId") DO NOTHING
                    """,
                    cancellationToken)
                .ConfigureAwait(false);

            if (affected == 0)
            {
                duplicates++;
            }
            else
            {
                accepted++;
            }
        }

        return Results.Ok(new TelemetryIngestionResponse
        {
            Accepted = accepted,
            Duplicates = duplicates
        });
    }

    private static IResult InvalidTelemetry(string detail)
    {
        return ApiProblem.Create(
            StatusCodes.Status422UnprocessableEntity,
            "invalid_telemetry",
            "Telemetry batch is invalid",
            detail);
    }
}
