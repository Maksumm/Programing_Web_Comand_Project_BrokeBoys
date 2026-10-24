using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Products;

internal static class ListProducts
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName("ListProducts");

    private static async Task<Ok<ProductResponse[]>> Handle(int? categoryId, AppDbContext db, CancellationToken ct)
    {
        var items = await db.Products.AsNoTracking()
            .Where(p => categoryId == null || p.CategoryId == categoryId)
            .OrderBy(p => p.Id)
            .ToListAsync(ct);
        return TypedResults.Ok(items.Select(ProductResponse.From).ToArray());
    }
}
