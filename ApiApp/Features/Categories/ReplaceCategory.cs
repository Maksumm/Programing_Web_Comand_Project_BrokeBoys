using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Categories;

public sealed record ReplaceCategoryRequest(string? NameUk, string? NameEn, string? DescriptionUk, string? DescriptionEn);

internal static class ReplaceCategory
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:int}", Handle)
            .WithName("ReplaceCategory")
            .RequireAuthorization(Policies.ManageCatalogue)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> Handle(int id, ReplaceCategoryRequest request, AppDbContext db, CancellationToken ct)
    {
        var errors = new ErrorBag()
            .RequiredText(nameof(request.NameUk), request.NameUk)
            .RequiredText(nameof(request.NameEn), request.NameEn)
            .OptionalText(nameof(request.DescriptionUk), request.DescriptionUk)
            .OptionalText(nameof(request.DescriptionEn), request.DescriptionEn);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
        {
            return Problems.NotFound($"Category {id} was not found.");
        }

        entity.NameUk = request.NameUk!.Trim();
        entity.NameEn = request.NameEn!.Trim();
        entity.DescriptionUk = request.DescriptionUk?.Trim() ?? string.Empty;
        entity.DescriptionEn = request.DescriptionEn?.Trim() ?? string.Empty;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PgErrors.Is(ex, PgErrors.UniqueViolation))
        {
            return Problems.Conflict("Another category already has this name.");
        }

        return TypedResults.NoContent();
    }
}
