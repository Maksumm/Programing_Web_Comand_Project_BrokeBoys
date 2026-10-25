using ApiApp.Common;

namespace ApiApp.Features.Products;

public static class ProductsEndpoints
{
    /// <summary>Підключає всі endpoint-и можливості «Products» до Program.cs одним викликом.</summary>
    public static IEndpointRouteBuilder MapProducts(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/products")
            .WithTags("Products")
            .RequireAuthorization(Policies.Read)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        ListProducts.Map(group);
        GetProduct.Map(group);
        CreateProduct.Map(group);
        ReplaceProduct.Map(group);
        DeleteProduct.Map(group);

        return routes;
    }
}
