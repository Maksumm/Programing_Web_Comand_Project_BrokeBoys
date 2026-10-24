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

        public Task<IEnumerable<Category>> GetCategoriesAsync() =>
            Task.FromResult<IEnumerable<Category>>(Array.Empty<Category>());

        public Task<IEnumerable<SeasonalPrice>> GetCalendarAsync() =>
            Task.FromResult<IEnumerable<SeasonalPrice>>(Array.Empty<SeasonalPrice>());

        public Task<IEnumerable<Subscription>> GetSubscriptionsAsync() =>
            Task.FromResult<IEnumerable<Subscription>>(Array.Empty<Subscription>());

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
