using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Prices;

public sealed record CreatePriceRequest(int? ProductId, int? Month, decimal? Price);

internal static class CreatePrice
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreatePrice")
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static Results<Created<PriceResponse>, ValidationProblem, ProblemHttpResult> Handle(CreatePriceRequest request, CatalogueStore store)
    {
        var errors = new ErrorBag()
            .RequiredId(nameof(request.ProductId), request.ProductId)
            .Month(nameof(request.Month), request.Month)
            .Price(nameof(request.Price), request.Price);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var result = store.AddPrice(request.ProductId!.Value, request.Month!.Value, request.Price!.Value);
        if (result.Status == StoreStatus.InvalidReference)
        {
            return Problems.Invalid(result.Field!, result.Detail!);
        }

        if (result.Status == StoreStatus.Conflict)
        {
            return Problems.Conflict(result.Detail!);
        }

        var response = PriceResponse.From(result.Value!);
        return TypedResults.Created($"/api/prices/{response.Id}", response);
    }
}
