using System.Globalization;
using Xunit;

namespace BlazorClient.Tests
{
    public class LocalizationTests
    {
        [Theory]
        [InlineData("uk-UA", "3,5", 3.5)]
        [InlineData("en-US", "3.5", 3.5)]
        public void Should_ParseDecimal_CorrectlyForCulture(string cultureName, string inputValue, decimal expected)
        {
            var culture = new CultureInfo(cultureName);

            var success = decimal.TryParse(inputValue, NumberStyles.Number, culture, out var result);

            Assert.True(success);
            Assert.Equal(expected, result);
        }
    }
}