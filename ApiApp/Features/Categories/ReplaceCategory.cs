using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Categories;

public sealed record ReplaceCategoryRequest(string? NameUk, string? NameEn, string? DescriptionUk, string? DescriptionEn);

internal static class ReplaceCategory
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:int}", Handle)
            .WithName("ReplaceCategory")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static Results<NoContent, ValidationProblem, ProblemHttpResult> Handle(int id, ReplaceCategoryRequest request, CatalogueStore store)
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

        var result = store.ReplaceCategory(id, request.NameUk!.Trim(), request.NameEn!.Trim(), request.DescriptionUk?.Trim() ?? string.Empty, request.DescriptionEn?.Trim() ?? string.Empty);
        if (result.Status == StoreStatus.NotFound)
        {
            return Problems.NotFound(result.Detail!);
        }

        if (result.Status == StoreStatus.Conflict)
        {
            return Problems.Conflict(result.Detail!);
        }

        return TypedResults.NoContent();
    }
}
