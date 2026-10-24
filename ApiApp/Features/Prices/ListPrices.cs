using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Prices;

internal static class ListPrices
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName("ListPrices");

    private static Ok<PriceResponse[]> Handle(int? productId, CatalogueStore store) =>
        TypedResults.Ok(store.ListPrices(productId).Select(PriceResponse.From).ToArray());
}
