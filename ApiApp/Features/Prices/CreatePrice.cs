using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Prices;

public sealed record CreatePriceRequest(int? ProductId, int? Month, decimal? Price);

internal static class CreatePrice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreatePrice")
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<PriceResponse>, ValidationProblem, ProblemHttpResult>> Handle(CreatePriceRequest request, AppDbContext db, CancellationToken ct)
    {
        var errors = new ErrorBag()
            .RequiredId(nameof(request.ProductId), request.ProductId)
            .Month(nameof(request.Month), request.Month)
            .Price(nameof(request.Price), request.Price);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        if (!await db.Products.AnyAsync(p => p.Id == request.ProductId, ct))
        {
            return Problems.Invalid(nameof(request.ProductId), "Product does not exist.");
        }

        var entity = new SeasonalPrice
        {
            ProductId = request.ProductId!.Value,
            Month = request.Month!.Value,
            Price = request.Price!.Value
        };
        db.SeasonalPrices.Add(entity);
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

        return TypedResults.Created($"/api/prices/{entity.Id}", PriceResponse.From(entity));
    }
}
