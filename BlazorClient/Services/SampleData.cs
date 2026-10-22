using BlazorClient.Models;

namespace BlazorClient.Services
{
    /// <summary>Демонстраційні дані, поки немає API: імітують запит із короткою затримкою.</summary>
    public static class SampleData
    {
        public static async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            await Task.Delay(300);
            return new List<Category>
            {
                new() { Id = 1, Name = "Дрова", Description = "Сезонні ціни на паливну деревину", SubscriptionsCount = 12 },
                new() { Id = 2, Name = "Шини", Description = "Літня та зимова гума", SubscriptionsCount = 8 },
                new() { Id = 3, Name = "Будматеріали", Description = "Цегла, цемент, пиломатеріали", SubscriptionsCount = 5 }
            };
        }

        public static async Task<IEnumerable<SeasonalPrice>> GetPricesAsync()
        {
            await Task.Delay(300);
            return new List<SeasonalPrice>
            {
                new() { Id = 1, Name = "Дрова, дуб", Price = 1500.00m, Month = "Жовтень" }
            };
        }

        public static async Task<IEnumerable<Subscription>> GetSubscriptionsAsync()
        {
            await Task.Delay(300);
            return new List<Subscription>
            {
                new() { Id = 1, Email = "user@example.com", CategoryName = "Дрова", Threshold = 90m, IsActive = true },
                new() { Id = 2, Email = "user@example.com", CategoryName = "Шини", Threshold = 85.5m, IsActive = true }
            };
        }
    }
}
