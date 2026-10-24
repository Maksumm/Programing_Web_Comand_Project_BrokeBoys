using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Products;

internal static class ListProducts
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName("ListProducts");

    private static Ok<ProductResponse[]> Handle(int? categoryId, CatalogueStore store) =>
        TypedResults.Ok(store.ListProducts(categoryId).Select(ProductResponse.From).ToArray());
}
