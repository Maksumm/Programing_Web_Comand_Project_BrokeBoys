using BlazorClient.Models;
using BlazorClient.Resources;
using Microsoft.Extensions.Localization;

namespace BlazorClient.Services
{
    /// <summary>Демонстраційні дані, поки немає API: імітують запит із короткою затримкою. Тексти беруться з .resx.</summary>
    public static class SampleData
    {
        public static async Task<IEnumerable<Category>> GetCategoriesAsync(IStringLocalizer<AppResources> l)
        {
            await Task.Delay(300);
            return new List<Category>
            {
                new() { Id = 1, Name = l["Firewood"], Description = l["CategoryFirewoodDescription"], SubscriptionsCount = 12 },
                new() { Id = 2, Name = l["Tires"], Description = l["CategoryTiresDescription"], SubscriptionsCount = 8 },
                new() { Id = 3, Name = l["BuildingMaterials"], Description = l["CategoryMaterialsDescription"], SubscriptionsCount = 5 }
            };
        }

        public static async Task<IEnumerable<SeasonalPrice>> GetPricesAsync(IStringLocalizer<AppResources> l)
        {
            await Task.Delay(300);
            return new List<SeasonalPrice>
            {
                new() { Id = 1, Name = l["ProductFirewoodOak"], Price = 1500.00m, Month = l["Month_Oct"] }
            };
        }

        public static async Task<IEnumerable<Subscription>> GetSubscriptionsAsync(IStringLocalizer<AppResources> l)
        {
            await Task.Delay(300);
            return new List<Subscription>
            {
                new() { Id = 1, Email = "user@example.com", CategoryName = l["Firewood"], Threshold = 90m, IsActive = true },
                new() { Id = 2, Email = "user@example.com", CategoryName = l["Tires"], Threshold = 85.5m, IsActive = true }
            };
        }
    }
}
