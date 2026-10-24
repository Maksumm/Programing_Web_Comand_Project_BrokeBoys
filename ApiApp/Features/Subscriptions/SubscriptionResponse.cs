using ApiApp.Data;

namespace ApiApp.Features.Subscriptions;

public sealed record SubscriptionResponse(int Id, string Email, int CategoryId, decimal? Threshold, bool IsActive)
{
    internal static SubscriptionResponse From(Subscription source) =>
        new(source.Id, source.Email, source.CategoryId, source.Threshold, source.IsActive);
}
