namespace BlazorClient.Models
{
    // Дані для екранів адміністратора: обидві мови, як їх зберігає сервер.
    public sealed record AdminCategory(int Id, string NameUk, string NameEn, string DescriptionUk, string DescriptionEn, int ProductsCount, int SubscriptionsCount);

    public sealed record AdminProduct(int Id, int CategoryId, string NameUk, string NameEn);

    public sealed record AdminPrice(int Id, int ProductId, int Month, decimal Price);

    public sealed record CategoryInput(string NameUk, string NameEn, string DescriptionUk, string DescriptionEn);

    public sealed record ProductInput(int CategoryId, string NameUk, string NameEn);

    public sealed record PriceInput(int ProductId, int Month, decimal Price);
}
