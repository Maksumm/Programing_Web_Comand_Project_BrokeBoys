using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Prices;

internal static class GetPrice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:int}", Handle)
            .WithName("GetPrice")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static Results<Ok<PriceResponse>, ProblemHttpResult> Handle(int id, CatalogueStore store)
    {
        var item = store.GetPrice(id);
        if (item is null)
        {
            return Problems.NotFound($"Price {id} was not found.");
        }

        return TypedResults.Ok(PriceResponse.From(item));
    }
}
