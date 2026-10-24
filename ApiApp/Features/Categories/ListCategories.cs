using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Categories;

internal static class ListCategories
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName("ListCategories");

    private static Ok<CategoryResponse[]> Handle(CatalogueStore store) =>
        TypedResults.Ok(store.ListCategories().Select(CategoryResponse.From).ToArray());
}
