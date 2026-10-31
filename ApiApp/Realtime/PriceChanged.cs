namespace ApiApp.Realtime;

/// <summary>
/// Повідомлення, яке отримують клієнти групи категорії: що сталося з ціною (Created, Updated, Deleted).
/// Назви товару обома мовами додано, щоб клієнт міг показати, про який товар ідеться.
/// </summary>
public sealed record PriceChanged(string Kind, int CategoryId, int ProductId, int Month, decimal? Price, string ProductNameUk, string ProductNameEn);
