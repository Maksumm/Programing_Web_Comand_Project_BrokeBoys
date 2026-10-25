using BlazorClient.Models;
using BlazorClient.Services;

namespace BlazorClient.Tests
{
    /// <summary>Підміна API для тестів сторінок: нічого не читає з мережі, лише запам'ятовує записи.</summary>
    internal sealed class FakeCatalogueApi : ICatalogueApi
    {
        private int _nextId = 100;

        /// <summary>Якщо true, усі операції запису повертають помилку «підписка вже існує».</summary>
        public bool FailWrites { get; set; }

        public List<int> DeletedIds { get; } = new();

        public List<(int Id, bool IsActive)> ActiveChanges { get; } = new();

        /// <summary>Дані, які «сервер» віддає при читанні (наприклад, після кнопки «Спробувати ще раз»).</summary>
        public List<Category> Categories { get; } = new();

        public List<SeasonalPrice> Prices { get; } = new();

        public List<Subscription> Subscriptions { get; } = new();

        /// <summary>Користувач, якого «сервер» повертає для /api/me (за замовчуванням — з обома ролями).</summary>
        public UserInfo? CurrentUser { get; set; } = new("Test User", "tester@example.com", new[] { "Admin", "Subscriber" });

        public int CurrentUserRequests { get; private set; }

        public Task<UserInfo?> GetCurrentUserAsync()
        {
            CurrentUserRequests++;
            return Task.FromResult(CurrentUser);
        }

        public Task<IEnumerable<Category>> GetCategoriesAsync() =>
            Task.FromResult<IEnumerable<Category>>(Categories.ToList());

        public Task<IEnumerable<SeasonalPrice>> GetCalendarAsync() =>
            Task.FromResult<IEnumerable<SeasonalPrice>>(Prices.ToList());

        public Task<IEnumerable<Subscription>> GetSubscriptionsAsync() =>
            Task.FromResult<IEnumerable<Subscription>>(Subscriptions.ToList());

        public Task<ApiResult<Subscription>> CreateSubscriptionAsync(SubscribeFormModel form)
        {
            if (FailWrites)
            {
                return Task.FromResult(ApiResult<Subscription>.Fail("ErrorSubscriptionExists"));
            }

            var created = new Subscription
            {
                Id = ++_nextId,
                Email = form.Email,
                CategoryName = form.Category,
                Threshold = form.Threshold,
                IsActive = true
            };
            return Task.FromResult(ApiResult<Subscription>.Ok(created));
        }

        public Task<ApiResult> SetSubscriptionActiveAsync(Subscription subscription, bool isActive)
        {
            ActiveChanges.Add((subscription.Id, isActive));
            return Task.FromResult(FailWrites ? ApiResult.Fail("ErrorApiUnavailable") : ApiResult.Ok());
        }

        public Task<ApiResult> DeleteSubscriptionAsync(int id)
        {
            DeletedIds.Add(id);
            return Task.FromResult(FailWrites ? ApiResult.Fail("ErrorApiUnavailable") : ApiResult.Ok());
        }
    }
}
