using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Prices;

public sealed record ReplacePriceRequest(int? ProductId, int? Month, decimal? Price);

internal static class ReplacePrice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:int}", Handle)
            .WithName("ReplacePrice")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> Handle(int id, ReplacePriceRequest request, AppDbContext db, CancellationToken ct)
    {
        var errors = new ErrorBag()
            .RequiredId(nameof(request.ProductId), request.ProductId)
            .Month(nameof(request.Month), request.Month)
            .Price(nameof(request.Price), request.Price);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var entity = await db.SeasonalPrices.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
        {
            return Problems.NotFound($"Price {id} was not found.");
        }

        if (!await db.Products.AnyAsync(p => p.Id == request.ProductId, ct))
        {
            return Problems.Invalid(nameof(request.ProductId), "Product does not exist.");
        }

        entity.ProductId = request.ProductId!.Value;
        entity.Month = request.Month!.Value;
        entity.Price = request.Price!.Value;
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

        return TypedResults.NoContent();
    }
}
