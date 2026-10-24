using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Categories;

internal static class GetCategory
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:int}", Handle)
            .WithName("GetCategory")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static Results<Ok<CategoryResponse>, ProblemHttpResult> Handle(int id, CatalogueStore store)
    {
        var item = store.GetCategory(id);
        if (item is null)
        {
            return Problems.NotFound($"Category {id} was not found.");
        }

        return TypedResults.Ok(CategoryResponse.From(item));
    }
}
