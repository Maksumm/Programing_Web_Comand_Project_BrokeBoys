using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Prices;

internal static class ListPrices
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName("ListPrices");

    private static async Task<Ok<PriceResponse[]>> Handle(int? productId, AppDbContext db, CancellationToken ct)
    {
        var items = await db.SeasonalPrices.AsNoTracking()
            .Where(p => productId == null || p.ProductId == productId)
            .OrderBy(p => p.ProductId).ThenBy(p => p.Month)
            .ToListAsync(ct);
        return TypedResults.Ok(items.Select(PriceResponse.From).ToArray());
    }
}
