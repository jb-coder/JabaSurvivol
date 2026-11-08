using System.Globalization;
using System.Text;
using System.Text.Json;
using Game.Data;
using JabaSurvivol.Contracts;
using JabaSurvivol.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game.Api.Saves;

public static class SaveEndpoints
{
    private const string PrimarySlot = SaveContract.PrimarySlot;

    public static IEndpointRouteBuilder MapSaveEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/game/saves").WithTags("Saves");

        group.MapGet("/primary", GetPrimaryAsync)
            .RequireAuthorization()
            .WithName("GetPrimaryCloudSave");

        group.MapPut("/primary", PutPrimaryAsync)
            .RequireAuthorization()
            .WithMetadata(new RequestSizeLimitAttribute(SaveContract.MaximumBodyBytes))
            .WithName("PutPrimaryCloudSave");

        return endpoints;
    }

    private static async Task<IResult> GetPrimaryAsync(
        HttpContext httpContext,
        GameDbContext database,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetAccountId(out var accountId))
        {
            return ApiProblem.Unauthorized();
        }

        var save = await database.CloudSaves
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.AccountId == accountId && candidate.Slot == PrimarySlot,
                cancellationToken)
            .ConfigureAwait(false);

        if (save is null)
        {
            return ApiProblem.NotFound("No cloud save exists for this account.");
        }

        var etag = CreateEtag(save.Revision);
        httpContext.Response.Headers.ETag = etag;

        if (MatchesEtag(httpContext.Request.Headers.IfNoneMatch, etag))
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        return Results.Ok(ToResponse(save, etag));
    }

    private static async Task<IResult> PutPrimaryAsync(
        HttpContext httpContext,
        GameDbContext database,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetAccountId(out var accountId))
        {
            return ApiProblem.Unauthorized();
        }

        string body;
        using (var reader = new StreamReader(
                   httpContext.Request.Body,
                   Encoding.UTF8,
                   detectEncodingFromByteOrderMarks: false,
                   bufferSize: 8192,
                   leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        if (Encoding.UTF8.GetByteCount(body) > SaveContract.MaximumBodyBytes)
        {
            return PayloadTooLarge();
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return InvalidSave("The request body is not valid JSON.");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !CloudSaveValidator.TryGetProperty(root, "baseRevision", out var baseRevisionElement) ||
                baseRevisionElement.ValueKind != JsonValueKind.Number ||
                !baseRevisionElement.TryGetInt64(out var baseRevision))
            {
                return InvalidSave("The request must contain a numeric baseRevision.");
            }

            if (!CloudSaveValidator.TryGetProperty(root, "document", out var profileDocument) ||
                profileDocument.ValueKind != JsonValueKind.Object)
            {
                return InvalidSave("The request must contain a document object.");
            }

            if (!CloudSaveValidator.TryValidate(profileDocument, out var schemaVersion, out var validationError))
            {
                return InvalidSave(validationError);
            }

            var rawDocument = profileDocument.GetRawText();
            var now = timeProvider.GetUtcNow();
            var save = await database.CloudSaves
                .SingleOrDefaultAsync(
                    candidate => candidate.AccountId == accountId && candidate.Slot == PrimarySlot,
                    cancellationToken)
                .ConfigureAwait(false);

            if (save is null)
            {
                if (baseRevision != 0)
                {
                    return Conflict(0, null);
                }

                save = new CloudSave
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    Slot = PrimarySlot,
                    Revision = 1,
                    SchemaVersion = schemaVersion,
                    Document = rawDocument,
                    UpdatedAt = now
                };

                database.CloudSaves.Add(save);

                try
                {
                    await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateException)
                {
                    var current = await database.CloudSaves
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            candidate => candidate.AccountId == accountId && candidate.Slot == PrimarySlot,
                            cancellationToken)
                        .ConfigureAwait(false);

                    return current is null
                        ? Conflict(0, null)
                        : Conflict(current.Revision, current.UpdatedAt);
                }

                var createdEtag = CreateEtag(save.Revision);
                httpContext.Response.Headers.ETag = createdEtag;
                return Results.Ok(ToResponse(save, createdEtag));
            }

            if (baseRevision != save.Revision)
            {
                return Conflict(save.Revision, save.UpdatedAt);
            }

            save.Revision += 1;
            save.SchemaVersion = schemaVersion;
            save.Document = rawDocument;
            save.UpdatedAt = now;

            try
            {
                await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                var current = await database.CloudSaves
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        candidate => candidate.AccountId == accountId && candidate.Slot == PrimarySlot,
                        cancellationToken)
                    .ConfigureAwait(false);

                return current is null
                    ? Conflict(0, null)
                    : Conflict(current.Revision, current.UpdatedAt);
            }

            var etag = CreateEtag(save.Revision);
            httpContext.Response.Headers.ETag = etag;
            return Results.Ok(ToResponse(save, etag));
        }
    }

    private static SaveResponse ToResponse(CloudSave save, string etag)
    {
        using var document = JsonDocument.Parse(save.Document);
        return new SaveResponse
        {
            Slot = save.Slot,
            Revision = save.Revision,
            SchemaVersion = save.SchemaVersion,
            UpdatedAt = save.UpdatedAt,
            ETag = etag,
            Document = document.RootElement.Clone()
        };
    }

    private static string CreateEtag(long revision)
    {
        return $"\"rev-{revision.ToString(CultureInfo.InvariantCulture)}\"";
    }

    private static bool MatchesEtag(string? ifNoneMatch, string etag)
    {
        if (string.IsNullOrWhiteSpace(ifNoneMatch))
        {
            return false;
        }

        return ifNoneMatch.Contains(etag, StringComparison.Ordinal) ||
               ifNoneMatch.Contains("*", StringComparison.Ordinal);
    }

    private static IResult Conflict(long currentRevision, DateTimeOffset? currentUpdatedAt)
    {
        return ApiProblem.Create(
            StatusCodes.Status409Conflict,
            "revision_conflict",
            "Cloud save revision conflict",
            "The save was updated by another session. Reload before writing.",
            new Dictionary<string, object?>
            {
                ["currentRevision"] = currentRevision,
                ["currentUpdatedAt"] = currentUpdatedAt?.ToString("O", CultureInfo.InvariantCulture)
            });
    }

    private static IResult PayloadTooLarge()
    {
        return ApiProblem.Create(
            StatusCodes.Status413PayloadTooLarge,
            "payload_too_large",
            "Cloud save is too large",
            $"The maximum request size is {SaveContract.MaximumBodyBytes} bytes.");
    }

    private static IResult InvalidSave(string detail)
    {
        return ApiProblem.Create(
            StatusCodes.Status422UnprocessableEntity,
            "invalid_save",
            "Cloud save is invalid",
            detail);
    }
}
