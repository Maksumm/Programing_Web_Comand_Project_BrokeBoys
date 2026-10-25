namespace ApiApp.Features.Me;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMe(this IEndpointRouteBuilder routes)
    {
        GetMe.Map(routes);
        return routes;
    }
}
