namespace ApiApp.Features.Subscriptions;

public static class SubscriptionsEndpoints
{
    /// <summary>Підключає всі endpoint-и можливості «Subscriptions» до Program.cs одним викликом.</summary>
    public static IEndpointRouteBuilder MapSubscriptions(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/subscriptions").WithTags("Subscriptions");

        ListSubscriptions.Map(group);
        GetSubscription.Map(group);
        CreateSubscription.Map(group);
        ReplaceSubscription.Map(group);
        DeleteSubscription.Map(group);

        return routes;
    }
}
