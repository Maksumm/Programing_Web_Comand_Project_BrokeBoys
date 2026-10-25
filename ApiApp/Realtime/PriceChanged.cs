namespace ApiApp.Realtime;

/// <summary>Повідомлення, яке отримують клієнти групи категорії: що сталося з ціною (Created, Updated, Deleted).</summary>
public sealed record PriceChanged(string Kind, int CategoryId, int ProductId, int Month, decimal? Price);
