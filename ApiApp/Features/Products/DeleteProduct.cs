using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Products;

internal static class DeleteProduct
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:int}", Handle)
            .WithName("DeleteProduct")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static Results<NoContent, ProblemHttpResult> Handle(int id, CatalogueStore store)
    {
        var result = store.DeleteProduct(id);
        if (result.Status == StoreStatus.NotFound)
        {
            return Problems.NotFound(result.Detail!);
        }

        return TypedResults.NoContent();
    }
}
