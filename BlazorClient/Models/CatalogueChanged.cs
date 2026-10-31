using BlazorClient.Services;

namespace BlazorClient.Models
{
    /// <summary>
    /// Повідомлення сервера про зміну товару чи категорії. Entity: Product або Category; Kind: Created, Updated або Deleted.
    /// Для категорії ProductId порожній, а назви — назви категорії.
    /// </summary>
    public sealed record CatalogueChanged(string Entity, string Kind, int CategoryId, int? ProductId, string NameUk, string NameEn)
    {
        /// <summary>Назва мовою інтерфейсу.</summary>
        public string Name => LocalizedName.Pick(NameUk, NameEn);
    }
}
