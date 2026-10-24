using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Products;

internal static class GetProduct
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:int}", Handle)
            .WithName("GetProduct")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static Results<Ok<ProductResponse>, ProblemHttpResult> Handle(int id, CatalogueStore store)
    {
        var item = store.GetProduct(id);
        if (item is null)
        {
            return Problems.NotFound($"Product {id} was not found.");
        }

        return TypedResults.Ok(ProductResponse.From(item));
    }
}
