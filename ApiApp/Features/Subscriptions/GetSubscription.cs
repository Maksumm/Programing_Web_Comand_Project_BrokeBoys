using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Subscriptions;

internal static class GetSubscription
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:int}", Handle)
            .WithName("GetSubscription")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static Results<Ok<SubscriptionResponse>, ProblemHttpResult> Handle(int id, CatalogueStore store)
    {
        var item = store.GetSubscription(id);
        if (item is null)
        {
            return Problems.NotFound($"Subscription {id} was not found.");
        }

        return TypedResults.Ok(SubscriptionResponse.From(item));
    }
}
