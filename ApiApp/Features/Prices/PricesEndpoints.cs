namespace ApiApp.Features.Prices;

public static class PricesEndpoints
{
    /// <summary>Підключає всі endpoint-и можливості «Prices» до Program.cs одним викликом.</summary>
    public static IEndpointRouteBuilder MapPrices(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/prices").WithTags("Prices");

        ListPrices.Map(group);
        GetPrice.Map(group);
        CreatePrice.Map(group);
        ReplacePrice.Map(group);
        DeletePrice.Map(group);

        return routes;
    }
}
