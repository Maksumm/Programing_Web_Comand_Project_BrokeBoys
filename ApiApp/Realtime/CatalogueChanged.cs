namespace ApiApp.Realtime;

/// <summary>
/// Повідомлення клієнтам групи категорії про зміну товару чи самої категорії.
/// Entity: Product або Category; Kind: Created, Updated або Deleted. Для категорії ProductId порожній, а назви — назви категорії.
/// Назви обома мовами, щоб клієнт показав їх мовою інтерфейсу (наприклад, у сповіщенні).
/// </summary>
public sealed record CatalogueChanged(string Entity, string Kind, int CategoryId, int? ProductId, string NameUk, string NameEn);
