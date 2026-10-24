using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Prices;

internal static class DeletePrice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:int}", Handle)
            .WithName("DeletePrice")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static Results<NoContent, ProblemHttpResult> Handle(int id, CatalogueStore store)
    {
        var result = store.DeletePrice(id);
        if (result.Status == StoreStatus.NotFound)
        {
            return Problems.NotFound(result.Detail!);
        }

        return TypedResults.NoContent();
    }
}
