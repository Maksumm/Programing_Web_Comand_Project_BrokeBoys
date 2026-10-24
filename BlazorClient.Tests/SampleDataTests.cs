using System.Text.RegularExpressions;
using BlazorClient.Services;

namespace BlazorClient.Tests
{
    public class SampleDataTests
    {
        private static readonly Regex Cyrillic = new("[\u0400-\u04FF]");

        [Theory]
        [InlineData("uk-UA")]
        [InlineData("en-US")]
        public async Task SampleData_UsesTextsOfSelectedCulture(string culture)
        {
            using var scope = new CultureScope(culture);
            await using var ctx = TestHelpers.CreateContext();
            var l = ctx.Localizer();

            var categories = (await SampleData.GetCategoriesAsync(l)).ToList();
            var prices = (await SampleData.GetPricesAsync(l)).ToList();
            var subscriptions = (await SampleData.GetSubscriptionsAsync(l)).ToList();

            Assert.Equal(l["Firewood"].Value, categories[0].Name);
            Assert.Equal(l["Tires"].Value, categories[1].Name);
            Assert.Equal(l["BuildingMaterials"].Value, categories[2].Name);
            Assert.Equal(l["ProductFirewoodOak"].Value, prices[0].Name);
            Assert.Equal(l["Firewood"].Value, subscriptions[0].CategoryName);
            Assert.Equal(l["Tires"].Value, subscriptions[1].CategoryName);
        }

        [Fact]
        public async Task SampleData_InEnglish_ContainsNoUkrainianText()
        {
            using var scope = new CultureScope("en-US");
            await using var ctx = TestHelpers.CreateContext();
            var l = ctx.Localizer();

            var texts = (await SampleData.GetCategoriesAsync(l)).SelectMany(c => new[] { c.Name, c.Description })
                .Concat((await SampleData.GetPricesAsync(l)).SelectMany(p => new[] { p.Name, p.Month }))
                .Concat((await SampleData.GetSubscriptionsAsync(l)).Select(s => s.CategoryName));

            Assert.DoesNotContain(texts, t => Cyrillic.IsMatch(t));
        }
    }
}
