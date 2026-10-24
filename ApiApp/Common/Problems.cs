using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Common;

/// <summary>Усі відповіді з помилкою мають тіло ProblemDetails (application/problem+json).</summary>
internal static class Problems
{
    public static ProblemHttpResult NotFound(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status404NotFound, title: "Not Found");

    public static ProblemHttpResult Conflict(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status409Conflict, title: "Conflict");

    public static ValidationProblem Invalid(string field, string detail) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [ErrorBag.CamelCase(field)] = [detail] });
}
