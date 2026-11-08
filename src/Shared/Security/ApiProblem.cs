using Microsoft.AspNetCore.Http;

namespace JabaSurvivol.Security;

/// <summary>
/// Creates consistent ProblemDetails responses with a stable machine-readable
/// <c>code</c> extension. Details never include internal exception text.
/// </summary>
public static class ApiProblem
{
    public static IResult Create(
        int statusCode,
        string code,
        string title,
        string? detail = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        var values = new Dictionary<string, object?>
        {
            ["code"] = code
        };

        if (extensions is not null)
        {
            foreach (var pair in extensions)
            {
                values[pair.Key] = pair.Value;
            }
        }

        return Results.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            extensions: values);
    }

    public static IResult Unauthorized(string detail = "Authentication is required.")
    {
        return Create(StatusCodes.Status401Unauthorized, "unauthorized", "Unauthorized", detail);
    }

    public static IResult Forbidden(string detail = "The account cannot access this resource.")
    {
        return Create(StatusCodes.Status403Forbidden, "forbidden", "Forbidden", detail);
    }

    public static IResult NotFound(string detail = "The resource was not found.")
    {
        return Create(StatusCodes.Status404NotFound, "not_found", "Not found", detail);
    }

    public static IResult Validation(string code, string detail)
    {
        return Create(StatusCodes.Status400BadRequest, code, "Invalid request", detail);
    }
}
