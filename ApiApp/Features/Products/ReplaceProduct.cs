using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Products;

public sealed record ReplaceProductRequest(int? CategoryId, string? NameUk, string? NameEn);

internal static class ReplaceProduct
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:int}", Handle)
            .WithName("ReplaceProduct")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static Results<NoContent, ValidationProblem, ProblemHttpResult> Handle(int id, ReplaceProductRequest request, CatalogueStore store)
    {
        var errors = new ErrorBag()
            .RequiredId(nameof(request.CategoryId), request.CategoryId)
            .RequiredText(nameof(request.NameUk), request.NameUk)
            .RequiredText(nameof(request.NameEn), request.NameEn);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var result = store.ReplaceProduct(id, request.CategoryId!.Value, request.NameUk!.Trim(), request.NameEn!.Trim());
        if (result.Status == StoreStatus.NotFound)
        {
            return Problems.NotFound(result.Detail!);
        }

        if (result.Status == StoreStatus.InvalidReference)
        {
            return Problems.Invalid(result.Field!, result.Detail!);
        }

        if (result.Status == StoreStatus.Conflict)
        {
            return Problems.Conflict(result.Detail!);
        }

        return TypedResults.NoContent();
    }
}
