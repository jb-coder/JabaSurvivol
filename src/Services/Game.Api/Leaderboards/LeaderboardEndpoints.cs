using Game.Data;
using JabaSurvivol.Contracts;
using JabaSurvivol.Security;
using Microsoft.EntityFrameworkCore;

namespace Game.Api.Leaderboards;

public static class LeaderboardEndpoints
{
    private const string SubmissionRateLimitPolicy = "leaderboard-submit";

    private const string BestRunSql = """
        SELECT DISTINCT ON ("AccountId") *
        FROM game."LeaderboardRuns"
        ORDER BY "AccountId", "HighestRound" DESC, "TotalKills" DESC, "DurationSeconds" ASC, "AchievedAt" ASC, "RunId" ASC
        """;

    public static IEndpointRouteBuilder MapLeaderboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/game/leaderboards/highest-round").WithTags("Leaderboards");

        group.MapPost("/runs", SubmitAsync)
            .RequireAuthorization()
            .RequireRateLimiting(SubmissionRateLimitPolicy)
            .WithName("SubmitLeaderboardRun");

        group.MapGet("", GetTopAsync)
            .AllowAnonymous()
            .WithName("GetHighestRoundLeaderboard");

        return endpoints;
    }

    private static async Task<IResult> SubmitAsync(
        LeaderboardSubmissionRequest? request,
        HttpContext httpContext,
        GameDbContext database,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!httpContext.User.TryGetAccountId(out var accountId))
        {
            return ApiProblem.Unauthorized();
        }

        if (request is null)
        {
            return ApiProblem.Validation("invalid_run", "A submission body is required.");
        }

        var existing = await database.LeaderboardRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                run => run.AccountId == accountId && run.RunId == request.RunId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return Results.Ok(ToResponse(existing));
        }

        if (!TryValidate(request, out var validationError))
        {
            return ApiProblem.Validation("invalid_run", validationError);
        }

        var accepted = new LeaderboardRun
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            RunId = request.RunId,
            Username = httpContext.User.GetUserName(),
            HighestRound = request.HighestRound,
            TotalKills = request.TotalKills,
            DurationSeconds = request.DurationSeconds,
            ClientVersion = request.ClientVersion!,
            AchievedAt = timeProvider.GetUtcNow()
        };

        database.LeaderboardRuns.Add(accepted);

        try
        {
            await database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            var winner = await database.LeaderboardRuns
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    run => run.AccountId == accountId && run.RunId == request.RunId,
                    cancellationToken)
                .ConfigureAwait(false);

            return winner is null
                ? ApiProblem.Create(
                    StatusCodes.Status409Conflict,
                    "run_conflict",
                    "Run could not be stored",
                    "Retry the submission.")
                : Results.Ok(ToResponse(winner));
        }

        return Results.Ok(ToResponse(accepted));
    }

    private static async Task<IResult> GetTopAsync(
        int? limit,
        GameDbContext database,
        CancellationToken cancellationToken)
    {
        var take = ClampLimit(limit);
        var bestRuns = await database.LeaderboardRuns
            .FromSqlRaw(BestRunSql)
            .AsNoTracking()
            .OrderByDescending(run => run.HighestRound)
            .ThenByDescending(run => run.TotalKills)
            .ThenBy(run => run.DurationSeconds)
            .ThenBy(run => run.AchievedAt)
            .ThenBy(run => run.RunId)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = bestRuns
            .Select((run, index) => new LeaderboardEntry
            {
                Rank = index + 1,
                Username = run.Username,
                HighestRound = run.HighestRound,
                TotalKills = run.TotalKills,
                DurationSeconds = run.DurationSeconds,
                AchievedAt = run.AchievedAt
            })
            .ToArray();

        return Results.Ok(new LeaderboardResponse
        {
            Board = LeaderboardContract.BoardName,
            Entries = entries
        });
    }

    public static int ClampLimit(int? limit)
    {
        if (limit is null or <= 0)
        {
            return LeaderboardContract.DefaultLimit;
        }

        return Math.Min(limit.Value, LeaderboardContract.MaximumLimit);
    }

    private static bool TryValidate(LeaderboardSubmissionRequest request, out string error)
    {
        if (request.RunId == Guid.Empty)
        {
            error = "runId must be a non-empty UUID.";
            return false;
        }

        if (request.HighestRound < LeaderboardContract.MinimumHighestRound ||
            request.HighestRound > LeaderboardContract.MaximumHighestRound)
        {
            error = $"highestRound must be between {LeaderboardContract.MinimumHighestRound} and {LeaderboardContract.MaximumHighestRound}.";
            return false;
        }

        if (request.TotalKills < 0 || request.TotalKills > LeaderboardContract.MaximumKills)
        {
            error = $"totalKills must be between 0 and {LeaderboardContract.MaximumKills}.";
            return false;
        }

        if (request.DurationSeconds < 1 ||
            request.DurationSeconds > LeaderboardContract.MaximumDurationSeconds)
        {
            error = $"durationSeconds must be between 1 and {LeaderboardContract.MaximumDurationSeconds}.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.ClientVersion) ||
            request.ClientVersion.Length > LeaderboardContract.MaximumClientVersionLength)
        {
            error = $"clientVersion must be 1-{LeaderboardContract.MaximumClientVersionLength} characters.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static LeaderboardSubmissionResponse ToResponse(LeaderboardRun run)
    {
        return new LeaderboardSubmissionResponse
        {
            Accepted = true,
            RunId = run.RunId,
            HighestRound = run.HighestRound,
            TotalKills = run.TotalKills,
            DurationSeconds = run.DurationSeconds,
            AchievedAt = run.AchievedAt
        };
    }
}
