namespace BlazorClient.Services
{
    // Форма відповідей сервера (camelCase JSON). Лише для CatalogueApi.
    internal sealed record CategoryDto(int Id, string NameUk, string NameEn, string DescriptionUk, string DescriptionEn, int ProductsCount, int SubscriptionsCount);

    internal sealed record ProductDto(int Id, int CategoryId, string NameUk, string NameEn);

    internal sealed record PriceDto(int Id, int ProductId, int Month, decimal Price);

    internal sealed record SubscriptionDto(int Id, string Email, int CategoryId, decimal? Threshold, bool IsActive);
}
