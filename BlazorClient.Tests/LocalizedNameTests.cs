using BlazorClient.Models;
using BlazorClient.Services;
using Xunit;

namespace BlazorClient.Tests
{
    public class LocalizedNameTests
    {
        [Theory]
        [InlineData("uk-UA", "Дуб")]
        [InlineData("en-US", "Oak")]
        public void Pick_UsesTheInterfaceLanguage(string culture, string expected)
        {
            using var scope = new CultureScope(culture);

            Assert.Equal(expected, LocalizedName.Pick("Дуб", "Oak"));
        }

        [Theory]
        [InlineData("uk-UA", "", "Oak", "Oak")]
        [InlineData("en-US", "Дуб", " ", "Дуб")]
        public void Pick_FallsBackToTheOtherLanguage_WhenTheWantedOneIsEmpty(string culture, string uk, string en, string expected)
        {
            using var scope = new CultureScope(culture);

            Assert.Equal(expected, LocalizedName.Pick(uk, en));
        }

        [Fact]
        public void PriceChanged_AndCatalogueChanged_ExposeTheNameInTheInterfaceLanguage()
        {
            var price = new PriceChanged("Updated", 1, 5, 7, 100m, "Дрова", "Firewood");
            var change = new CatalogueChanged("Product", "Created", 1, 5, "Брикети", "Briquettes");

            using (new CultureScope("uk-UA"))
            {
                Assert.Equal("Дрова", price.ProductName);
                Assert.Equal("Брикети", change.Name);
            }

            using (new CultureScope("en-US"))
            {
                Assert.Equal("Firewood", price.ProductName);
                Assert.Equal("Briquettes", change.Name);
            }
        }
    }
}
