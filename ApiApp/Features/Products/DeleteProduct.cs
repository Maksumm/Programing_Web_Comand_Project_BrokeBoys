using ApiApp.Common;
using ApiApp.Data;
using ApiApp.Realtime;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Products;

internal static class DeleteProduct
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:int}", Handle)
            .WithName("DeleteProduct")
            .RequireAuthorization(Policies.ManageCatalogue)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(int id, AppDbContext db, ICatalogueNotifier notifier, CancellationToken ct)
    {
        // Назву й категорію потрібно знати до видалення, щоб повідомити потрібну групу.
        var existing = await db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductRef(p.Id, p.CategoryId, p.NameUk, p.NameEn))
            .FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            return Problems.NotFound($"Product {id} was not found.");
        }

        var removed = await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync(ct); // ціни видаляє каскад бази
        if (removed == 0)
        {
            return Problems.NotFound($"Product {id} was not found.");
        }

        await notifier.NotifyProductAsync("Deleted", existing, [existing.CategoryId], ct);
        return TypedResults.NoContent();
    }
}
