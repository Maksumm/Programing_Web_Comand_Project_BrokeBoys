namespace BlazorClient.Models
{
    /// <summary>Повідомлення сервера про зміну ціни (Kind: Created, Updated або Deleted).</summary>
    public sealed record PriceChanged(string Kind, int CategoryId, int ProductId, int Month, decimal? Price);
}
