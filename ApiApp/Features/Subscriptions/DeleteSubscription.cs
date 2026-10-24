using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Subscriptions;

internal static class DeleteSubscription
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:int}", Handle)
            .WithName("DeleteSubscription")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static Results<NoContent, ProblemHttpResult> Handle(int id, CatalogueStore store)
    {
        var result = store.DeleteSubscription(id);
        if (result.Status == StoreStatus.NotFound)
        {
            return Problems.NotFound(result.Detail!);
        }

        return TypedResults.NoContent();
    }
}
