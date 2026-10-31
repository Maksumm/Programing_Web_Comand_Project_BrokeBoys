using BlazorClient.Services;

namespace BlazorClient.Models
{
    /// <summary>
    /// Повідомлення сервера про зміну ціни (Kind: Created, Updated або Deleted). Назви товару обома мовами сервер додає,
    /// щоб повідомлення могло назвати товар; старий сервер їх не надсилає — тоді вони порожні.
    /// </summary>
    public sealed record PriceChanged(string Kind, int CategoryId, int ProductId, int Month, decimal? Price, string ProductNameUk = "", string ProductNameEn = "")
    {
        /// <summary>Назва товару мовою інтерфейсу (порожня, якщо сервер її не надіслав).</summary>
        public string ProductName => LocalizedName.Pick(ProductNameUk, ProductNameEn);
    }
}
