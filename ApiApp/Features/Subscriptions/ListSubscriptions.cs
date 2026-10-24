using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Subscriptions;

internal static class ListSubscriptions
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName("ListSubscriptions");

    private static Ok<SubscriptionResponse[]> Handle(string? email, CatalogueStore store) =>
        TypedResults.Ok(store.ListSubscriptions(email).Select(SubscriptionResponse.From).ToArray());
}
