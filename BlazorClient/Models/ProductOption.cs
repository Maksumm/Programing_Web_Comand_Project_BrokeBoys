namespace BlazorClient.Models
{
    /// <summary>Товар для списку вибору на екрані «Календар»; назва вже в мові інтерфейсу.</summary>
    public sealed record ProductOption(int Id, int CategoryId, string Name);
}
