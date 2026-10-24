using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Subscriptions;

public sealed record CreateSubscriptionRequest(string? Email, int? CategoryId, decimal? Threshold, bool IsActive = true);

internal static class CreateSubscription
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreateSubscription")
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static Results<Created<SubscriptionResponse>, ValidationProblem, ProblemHttpResult> Handle(CreateSubscriptionRequest request, CatalogueStore store)
    {
        var errors = new ErrorBag()
            .Email(nameof(request.Email), request.Email)
            .RequiredId(nameof(request.CategoryId), request.CategoryId)
            .Threshold(nameof(request.Threshold), request.Threshold);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var result = store.AddSubscription(request.Email!.Trim(), request.CategoryId!.Value, request.Threshold, request.IsActive);
        if (result.Status == StoreStatus.InvalidReference)
        {
            return Problems.Invalid(result.Field!, result.Detail!);
        }

        if (result.Status == StoreStatus.Conflict)
        {
            return Problems.Conflict(result.Detail!);
        }

        var response = SubscriptionResponse.From(result.Value!);
        return TypedResults.Created($"/api/subscriptions/{response.Id}", response);
    }
}
