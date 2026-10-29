using BlazorClient.Models;

namespace BlazorClient.Services
{
    /// <summary>Усе, що клієнт отримує від сервера і надсилає йому. Читання кидають виняток при збої (екран покаже стан «помилка»), запис повертає <see cref="ApiResult"/>.</summary>
    public interface ICatalogueApi
    {
        Task<IEnumerable<Category>> GetCategoriesAsync();

        /// <summary>Ціни першого товару за 12 місяців для екрана «Календар».</summary>
        Task<IEnumerable<SeasonalPrice>> GetCalendarAsync();

        /// <summary>Ціни вибраного товару за 12 місяців для екрана «Календар».</summary>
        Task<IEnumerable<SeasonalPrice>> GetCalendarAsync(int productId);

        /// <summary>Усі товари (для списку вибору в календарі).</summary>
        Task<IEnumerable<ProductOption>> GetProductsAsync();

        Task<IEnumerable<Subscription>> GetSubscriptionsAsync();

        /// <summary>Ім'я, пошта й ролі користувача за токеном; <c>null</c>, якщо сервер недоступний або відмовив.</summary>
        Task<UserInfo?> GetCurrentUserAsync();

        Task<ApiResult<Subscription>> CreateSubscriptionAsync(SubscribeFormModel form);

        /// <summary>Підписка на категорію за її ідентифікатором (кнопка «Підписатися» в календарі).</summary>
        Task<ApiResult<Subscription>> SubscribeToCategoryAsync(int categoryId, string email, decimal? threshold = null);

        Task<ApiResult> SetSubscriptionActiveAsync(Subscription subscription, bool isActive);

        Task<ApiResult> DeleteSubscriptionAsync(int id);
    }
}
