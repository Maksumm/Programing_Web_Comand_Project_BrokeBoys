using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Prices;

public sealed record ReplacePriceRequest(int? ProductId, int? Month, decimal? Price);

internal static class ReplacePrice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:int}", Handle)
            .WithName("ReplacePrice")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static Results<NoContent, ValidationProblem, ProblemHttpResult> Handle(int id, ReplacePriceRequest request, CatalogueStore store)
    {
        var errors = new ErrorBag()
            .RequiredId(nameof(request.ProductId), request.ProductId)
            .Month(nameof(request.Month), request.Month)
            .Price(nameof(request.Price), request.Price);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var result = store.ReplacePrice(id, request.ProductId!.Value, request.Month!.Value, request.Price!.Value);
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
