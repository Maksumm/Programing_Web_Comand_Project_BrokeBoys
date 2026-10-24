using ApiApp.Data;

namespace ApiApp.Features.Products;

public sealed record ProductResponse(int Id, int CategoryId, string NameUk, string NameEn)
{
    internal static ProductResponse From(Product source) =>
        new(source.Id, source.CategoryId, source.NameUk, source.NameEn);
}
