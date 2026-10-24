using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Products;

public sealed record ReplaceProductRequest(int? CategoryId, string? NameUk, string? NameEn);

internal static class ReplaceProduct
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:int}", Handle)
            .WithName("ReplaceProduct")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> Handle(int id, ReplaceProductRequest request, AppDbContext db, CancellationToken ct)
    {
        var errors = new ErrorBag()
            .RequiredId(nameof(request.CategoryId), request.CategoryId)
            .RequiredText(nameof(request.NameUk), request.NameUk)
            .RequiredText(nameof(request.NameEn), request.NameEn);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
        {
            return Problems.NotFound($"Product {id} was not found.");
        }

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
        {
            return Problems.Invalid(nameof(request.CategoryId), "Category does not exist.");
        }

        entity.CategoryId = request.CategoryId!.Value;
        entity.NameUk = request.NameUk!.Trim();
        entity.NameEn = request.NameEn!.Trim();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PgErrors.Is(ex, PgErrors.UniqueViolation))
        {
            return Problems.Conflict("This category already has a product with this name.");
        }
        catch (DbUpdateException ex) when (PgErrors.Is(ex, PgErrors.ForeignKeyViolation))
        {
            return Problems.Invalid("CategoryId", "Category does not exist.");
        }

        return TypedResults.NoContent();
    }
}
