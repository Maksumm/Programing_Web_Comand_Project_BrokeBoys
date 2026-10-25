using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Categories;

internal static class ListCategories
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName("ListCategories");

    private static async Task<Ok<CategoryResponse[]>> Handle(AppDbContext db, CancellationToken ct)
    {
        var items = await db.Categories.AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(CategoryResponse.Projection)
            .ToArrayAsync(ct);
        return TypedResults.Ok(items);
    }
}
