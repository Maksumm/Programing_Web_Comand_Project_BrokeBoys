using ApiApp.Common;
using ApiApp.Data;
using ApiApp.Realtime;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Categories;

internal static class DeleteCategory
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:int}", Handle)
            .WithName("DeleteCategory")
            .RequireAuthorization(Policies.ManageCatalogue)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(int id, AppDbContext db, ICatalogueNotifier notifier, CancellationToken ct)
    {
        try
        {
            // Назви потрібно знати до видалення: після нього вже нічого не прочитати.
            var existing = await db.Categories.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new { c.NameUk, c.NameEn })
                .FirstOrDefaultAsync(ct);
            var removed = existing is null ? 0 : await db.Categories.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
            if (existing is null || removed == 0)
            {
                return Problems.NotFound($"Category {id} was not found.");
            }

            await notifier.NotifyCategoryAsync("Deleted", id, existing.NameUk, existing.NameEn, ct);
        }
        catch (Exception ex) when (PgErrors.IsReferenceViolation(ex))
        {
            return Problems.Conflict("The category still has products; delete or move them first.");
        }

        return TypedResults.NoContent();
    }
}
