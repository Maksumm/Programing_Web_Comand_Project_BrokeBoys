using Bunit;
using Xunit;
using BlazorClient.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace BlazorClient.Tests
{
    public class LocalizationTests : BunitContext
    {
        public LocalizationTests()
        {
            Services.AddLocalization();
        }

        [Fact]
        public void Should_Return_Ukrainian_Translation()
        {
            CultureInfo.CurrentCulture = new CultureInfo("uk-UA");
            CultureInfo.CurrentUICulture = new CultureInfo("uk-UA");

            var localizer = Services.BuildServiceProvider().GetRequiredService<IStringLocalizer<AppResources>>();

            Assert.NotNull(localizer["MySubscriptions"].Value);
        }
    }
}