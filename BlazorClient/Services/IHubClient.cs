using BlazorClient.Models;

namespace BlazorClient.Services
{
    /// <summary>Тонка обгортка над HubConnection, щоб логіку «наживо» можна було перевірити тестами без мережі.</summary>
    public interface IHubClient : IAsyncDisposable
    {
        event Func<Task>? Reconnecting;

        event Func<Task>? Reconnected;

        Task StartAsync();

        /// <summary>Викликає метод JoinCategory на сервері.</summary>
        Task JoinAsync(int categoryId);

        IDisposable OnPriceChanged(Action<PriceChanged> handler);
    }
}
