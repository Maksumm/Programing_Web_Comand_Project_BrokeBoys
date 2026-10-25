using System.Linq.Expressions;
using ApiApp.Data;

namespace ApiApp.Features.Categories;

public sealed record CategoryResponse(int Id, string NameUk, string NameEn, string DescriptionUk, string DescriptionEn, int ProductsCount, int SubscriptionsCount)
{
    internal static readonly Expression<Func<Category, CategoryResponse>> Projection = c => new CategoryResponse(
        c.Id, c.NameUk, c.NameEn, c.DescriptionUk, c.DescriptionEn, c.Products.Count, c.Subscriptions.Count);

    internal static CategoryResponse From(Category source, int productsCount, int subscriptionsCount) =>
        new(source.Id, source.NameUk, source.NameEn, source.DescriptionUk, source.DescriptionEn, productsCount, subscriptionsCount);
}
