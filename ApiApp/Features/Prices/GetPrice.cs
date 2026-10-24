using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Prices;

internal static class GetPrice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:int}", Handle)
            .WithName("GetPrice")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<PriceResponse>, ProblemHttpResult>> Handle(int id, AppDbContext db, CancellationToken ct)
    {
        var item = await db.SeasonalPrices.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (item is null)
        {
            return Problems.NotFound($"Price {id} was not found.");
        }

        return TypedResults.Ok(PriceResponse.From(item));
    }
}
