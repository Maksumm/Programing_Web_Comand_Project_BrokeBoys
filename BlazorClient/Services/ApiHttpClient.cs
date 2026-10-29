namespace BlazorClient.Services
{
    /// <summary>
    /// Один HttpClient до API (з токеном) на всі клієнти API у межах сесії: обробник запитів можна підключити лише до одного HttpClient.
    /// </summary>
    public sealed record ApiHttpClient(HttpClient Client);
}
