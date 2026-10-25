using ApiApp.Common;
using ApiApp.Data;
using ApiApp.Realtime;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Prices;

public sealed record ReplacePriceRequest(int? ProductId, int? Month, decimal? Price);

internal static class ReplacePrice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:int}", Handle)
            .WithName("ReplacePrice")
            .RequireAuthorization(Policies.ManageCatalogue)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> Handle(
        int id, ReplacePriceRequest request, AppDbContext db, IPriceNotifier notifier, CancellationToken ct)
    {
        var errors = new ErrorBag()
            .RequiredId(nameof(request.ProductId), request.ProductId)
            .Month(nameof(request.Month), request.Month)
            .Price(nameof(request.Price), request.Price);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var entity = await db.SeasonalPrices.Include(p => p.Product).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
        {
            return Problems.NotFound($"Price {id} was not found.");
        }

        var newCategoryId = await db.Products.AsNoTracking()
            .Where(p => p.Id == request.ProductId)
            .Select(p => (int?)p.CategoryId)
            .FirstOrDefaultAsync(ct);
        if (newCategoryId is null)
        {
            return Problems.Invalid(nameof(request.ProductId), "Product does not exist.");
        }

        var oldCategoryId = entity.Product.CategoryId;
        entity.ProductId = request.ProductId!.Value;
        entity.Month = request.Month!.Value;
        entity.Price = request.Price!.Value;

        // Повторний PUT з тими самими значеннями нічого не змінює — і нічого не розсилає.
        var changed = db.ChangeTracker.HasChanges();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PgErrors.Is(ex, PgErrors.UniqueViolation))
        {
            return Problems.Conflict("This product already has a price for this month.");
        }
        catch (DbUpdateException ex) when (PgErrors.Is(ex, PgErrors.ForeignKeyViolation))
        {
            return Problems.Invalid("ProductId", "Product does not exist.");
        }

        if (changed)
        {
            // Ціна могла перейти до товару іншої категорії: повідомляємо обидві категорії.
            await notifier.NotifyAsync("Updated", entity.ProductId, entity.Month, entity.Price, [oldCategoryId, newCategoryId.Value], ct);
        }

        return TypedResults.NoContent();
    }
}
