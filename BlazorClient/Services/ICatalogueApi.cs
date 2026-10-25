using BlazorClient.Models;

namespace BlazorClient.Services
{
    /// <summary>Усе, що клієнт отримує від сервера і надсилає йому. Читання кидають виняток при збої (екран покаже стан «помилка»), запис повертає <see cref="ApiResult"/>.</summary>
    public interface ICatalogueApi
    {
        Task<IEnumerable<Category>> GetCategoriesAsync();

        /// <summary>Ціни першого товару за 12 місяців для екрана «Календар».</summary>
        Task<IEnumerable<SeasonalPrice>> GetCalendarAsync();

        Task<IEnumerable<Subscription>> GetSubscriptionsAsync();

        Task<ApiResult<Subscription>> CreateSubscriptionAsync(SubscribeFormModel form);

        Task<ApiResult> SetSubscriptionActiveAsync(Subscription subscription, bool isActive);

        Task<ApiResult> DeleteSubscriptionAsync(int id);
    }
}
