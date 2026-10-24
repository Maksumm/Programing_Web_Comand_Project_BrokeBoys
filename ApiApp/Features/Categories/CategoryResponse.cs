using ApiApp.Data;

namespace ApiApp.Features.Categories;

public sealed record CategoryResponse(int Id, string NameUk, string NameEn, string DescriptionUk, string DescriptionEn, int ProductsCount, int SubscriptionsCount)
{
    internal static CategoryResponse From(CategoryView source) =>
        new(source.Category.Id, source.Category.NameUk, source.Category.NameEn, source.Category.DescriptionUk, source.Category.DescriptionEn, source.ProductsCount, source.SubscriptionsCount);
}
