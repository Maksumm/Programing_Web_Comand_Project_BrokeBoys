using ApiApp.Data;

namespace ApiApp.Features.Subscriptions;

public sealed record SubscriptionResponse(int Id, string Email, int CategoryId, decimal? Threshold, bool IsActive, string CategoryNameUk, string CategoryNameEn)
{
    internal static SubscriptionResponse From(Subscription source, Category category) =>
        new(source.Id, source.Email, source.CategoryId, source.Threshold, source.IsActive, category.NameUk, category.NameEn);

    /// <summary>Потребує завантаженої категорії (Include).</summary>
    internal static SubscriptionResponse From(Subscription source) => From(source, source.Category);
}
