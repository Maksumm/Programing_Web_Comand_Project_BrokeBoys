using ApiApp.Common;
using ApiApp.Data;
using ApiApp.Realtime;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Prices;

internal static class DeletePrice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:int}", Handle)
            .WithName("DeletePrice")
            .RequireAuthorization(Policies.ManageCatalogue)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(int id, AppDbContext db, IPriceNotifier notifier, CancellationToken ct)
    {
        // Категорію й товар потрібно знати до видалення, щоб повідомити потрібну групу.
        var existing = await db.SeasonalPrices.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.Month, Product = new ProductRef(p.ProductId, p.Product.CategoryId, p.Product.NameUk, p.Product.NameEn) })
            .FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            return Problems.NotFound($"Price {id} was not found.");
        }

        var removed = await db.SeasonalPrices.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
        if (removed == 0)
        {
            return Problems.NotFound($"Price {id} was not found.");
        }

        await notifier.NotifyAsync("Deleted", existing.Product, existing.Month, null, [existing.Product.CategoryId], ct);
        return TypedResults.NoContent();
    }
}
