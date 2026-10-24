using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Subscriptions;

public sealed record ReplaceSubscriptionRequest(string? Email, int? CategoryId, decimal? Threshold, bool? IsActive);

internal static class ReplaceSubscription
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:int}", Handle)
            .WithName("ReplaceSubscription")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static Results<NoContent, ValidationProblem, ProblemHttpResult> Handle(int id, ReplaceSubscriptionRequest request, CatalogueStore store)
    {
        var errors = new ErrorBag()
            .Email(nameof(request.Email), request.Email)
            .RequiredId(nameof(request.CategoryId), request.CategoryId)
            .Threshold(nameof(request.Threshold), request.Threshold)
            .RequiredBool(nameof(request.IsActive), request.IsActive);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var result = store.ReplaceSubscription(id, request.Email!.Trim(), request.CategoryId!.Value, request.Threshold, request.IsActive!.Value);
        if (result.Status == StoreStatus.NotFound)
        {
            return Problems.NotFound(result.Detail!);
        }

        if (result.Status == StoreStatus.InvalidReference)
        {
            return Problems.Invalid(result.Field!, result.Detail!);
        }

        if (result.Status == StoreStatus.Conflict)
        {
            return Problems.Conflict(result.Detail!);
        }

        return TypedResults.NoContent();
    }
}
