using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Categories;

public sealed record CreateCategoryRequest(string? NameUk, string? NameEn, string? DescriptionUk, string? DescriptionEn);

internal static class CreateCategory
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreateCategory")
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<CategoryResponse>, ValidationProblem, ProblemHttpResult>> Handle(CreateCategoryRequest request, AppDbContext db, CancellationToken ct)
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

        var entity = new Category
        {
            NameUk = request.NameUk!.Trim(),
            NameEn = request.NameEn!.Trim(),
            DescriptionUk = request.DescriptionUk?.Trim() ?? string.Empty,
            DescriptionEn = request.DescriptionEn?.Trim() ?? string.Empty
        };
        db.Categories.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PgErrors.Is(ex, PgErrors.UniqueViolation))
        {
            return Problems.Conflict("A category with this name already exists.");
        }

        return TypedResults.Created($"/api/categories/{entity.Id}", CategoryResponse.From(entity, 0, 0));
    }
}
