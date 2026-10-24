using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Categories;

public sealed record CreateCategoryRequest(string? NameUk, string? NameEn, string? DescriptionUk, string? DescriptionEn);

internal static class CreateCategory
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreateCategory")
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static Results<Created<CategoryResponse>, ValidationProblem, ProblemHttpResult> Handle(CreateCategoryRequest request, CatalogueStore store)
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

        var result = store.AddCategory(request.NameUk!.Trim(), request.NameEn!.Trim(), request.DescriptionUk?.Trim() ?? string.Empty, request.DescriptionEn?.Trim() ?? string.Empty);
        if (result.Status == StoreStatus.Conflict)
        {
            return Problems.Conflict(result.Detail!);
        }

        var response = CategoryResponse.From(result.Value!);
        return TypedResults.Created($"/api/categories/{response.Id}", response);
    }
}
