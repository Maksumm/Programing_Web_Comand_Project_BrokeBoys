namespace ApiApp.Features.Categories;

public static class CategoriesEndpoints
{
    /// <summary>Підключає всі endpoint-и можливості «Categories» до Program.cs одним викликом.</summary>
    public static IEndpointRouteBuilder MapCategories(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/categories").WithTags("Categories");

        ListCategories.Map(group);
        GetCategory.Map(group);
        CreateCategory.Map(group);
        ReplaceCategory.Map(group);
        DeleteCategory.Map(group);

        return routes;
    }
}
