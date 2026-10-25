using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Products;

public sealed record CreateProductRequest(int? CategoryId, string? NameUk, string? NameEn);

internal static class CreateProduct
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreateProduct")
            .RequireAuthorization(Policies.ManageCatalogue)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<ProductResponse>, ValidationProblem, ProblemHttpResult>> Handle(CreateProductRequest request, AppDbContext db, CancellationToken ct)
    {
        var errors = new ErrorBag()
            .RequiredId(nameof(request.CategoryId), request.CategoryId)
            .RequiredText(nameof(request.NameUk), request.NameUk)
            .RequiredText(nameof(request.NameEn), request.NameEn);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
        {
            return Problems.Invalid(nameof(request.CategoryId), "Category does not exist.");
        }

        var entity = new Product
        {
            CategoryId = request.CategoryId!.Value,
            NameUk = request.NameUk!.Trim(),
            NameEn = request.NameEn!.Trim()
        };
        db.Products.Add(entity);
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

        return TypedResults.Created($"/api/products/{entity.Id}", ProductResponse.From(entity));
    }
}
