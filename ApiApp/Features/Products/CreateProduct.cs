using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Products;

public sealed record CreateProductRequest(int? CategoryId, string? NameUk, string? NameEn);

internal static class CreateProduct
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreateProduct")
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static Results<Created<ProductResponse>, ValidationProblem, ProblemHttpResult> Handle(CreateProductRequest request, CatalogueStore store)
    {
        var errors = new ErrorBag()
            .RequiredId(nameof(request.CategoryId), request.CategoryId)
            .RequiredText(nameof(request.NameUk), request.NameUk)
            .RequiredText(nameof(request.NameEn), request.NameEn);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var result = store.AddProduct(request.CategoryId!.Value, request.NameUk!.Trim(), request.NameEn!.Trim());
        if (result.Status == StoreStatus.InvalidReference)
        {
            return Problems.Invalid(result.Field!, result.Detail!);
        }

        if (result.Status == StoreStatus.Conflict)
        {
            return Problems.Conflict(result.Detail!);
        }

        var response = ProductResponse.From(result.Value!);
        return TypedResults.Created($"/api/products/{response.Id}", response);
    }
}
