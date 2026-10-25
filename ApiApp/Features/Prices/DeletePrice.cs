using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Prices;

internal static class DeletePrice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:int}", Handle)
            .WithName("DeletePrice")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(int id, AppDbContext db, CancellationToken ct)
    {
        var removed = await db.SeasonalPrices.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
        if (removed == 0)
        {
            return Problems.NotFound($"Price {id} was not found.");
        }

        return TypedResults.NoContent();
    }
}
