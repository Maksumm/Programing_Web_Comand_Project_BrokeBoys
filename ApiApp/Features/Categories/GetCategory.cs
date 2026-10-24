using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Categories;

internal static class GetCategory
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:int}", Handle)
            .WithName("GetCategory")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<CategoryResponse>, ProblemHttpResult>> Handle(int id, AppDbContext db, CancellationToken ct)
    {
        var item = await db.Categories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(CategoryResponse.Projection)
            .FirstOrDefaultAsync(ct);
        if (item is null)
        {
            return Problems.NotFound($"Category {id} was not found.");
        }

        return TypedResults.Ok(item);
    }
}
