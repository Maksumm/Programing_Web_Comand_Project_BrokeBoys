using ApiApp.Data;

namespace ApiApp.Features.Prices;

public sealed record PriceResponse(int Id, int ProductId, int Month, decimal Price)
{
    internal static PriceResponse From(SeasonalPrice source) =>
        new(source.Id, source.ProductId, source.Month, source.Price);
}
