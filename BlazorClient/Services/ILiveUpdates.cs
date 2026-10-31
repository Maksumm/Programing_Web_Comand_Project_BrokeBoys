using BlazorClient.Models;

namespace BlazorClient.Services
{
    /// <summary>Оновлення «наживо»: клієнт підписується на групи категорій і отримує повідомлення про зміни цін.</summary>
    public interface ILiveUpdates
    {
        event Action<PriceChanged>? PriceChanged;

        /// <summary>Змінився, додався чи зник товар або змінилася категорія (лише групи, до яких клієнт приєднаний).</summary>
        event Action<CatalogueChanged>? CatalogueChanged;

        /// <summary>Змінилося, чи активне з'єднання (старт, обрив, повернення).</summary>
        event Action? StateChanged;

        /// <summary>З'єднання є, і клієнт приєднаний принаймні до однієї групи.</summary>
        bool IsLive { get; }

        /// <summary>Запускає з'єднання (якщо ще не запущене) і приєднує до групи категорії. Якщо сервер недоступний, нічого не кидає: «наживо» — необов'язкова можливість.</summary>
        Task JoinCategoryAsync(int categoryId);
    }
}
