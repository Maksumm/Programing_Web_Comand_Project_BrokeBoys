using BlazorClient.Components;
using BlazorClient.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests
{
    public class AdminPricesPanelTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        private static FakeAdminApi Seeded(BunitContext ctx)
        {
            var api = ctx.Services.GetRequiredService<FakeAdminApi>();
            api.Products.Add(new AdminProduct(10, 1, "Дуб", "Oak"));
            api.Products.Add(new AdminProduct(11, 1, "Береза", "Birch"));
            api.Prices.Add(new AdminPrice(1, 10, 1, 1700m));
            api.Prices.Add(new AdminPrice(2, 10, 2, 1650m));
            api.Prices.Add(new AdminPrice(3, 11, 5, 900m));
            return api;
        }

        [Fact]
        public async Task ListsThePricesOfTheFirstProduct_InMonthOrder()
        {
            await using var ctx = TestHelpers.CreateContext();
            Seeded(ctx);

            var cut = ctx.Render<AdminPricesPanel>();

            var rows = cut.FindAll("tbody tr");
            Assert.Equal(2, rows.Count);
            Assert.Contains("1700", rows[0].TextContent);
            Assert.Contains("1650", rows[1].TextContent);
        }

        [Fact]
        public async Task MonthDropdown_StartsAtTheFirstMonthWithoutAPrice()
        {
            await using var ctx = TestHelpers.CreateContext();
            Seeded(ctx);

            var cut = ctx.Render<AdminPricesPanel>();

            Assert.Equal(12, cut.FindAll("#price-month option").Count);
            Assert.Equal("3", cut.Find("#price-month").SelectedValue());
        }

        [Fact]
        public async Task SwitchingTheProduct_LoadsItsPrices()
        {
            await using var ctx = TestHelpers.CreateContext();
            Seeded(ctx);

            var cut = ctx.Render<AdminPricesPanel>();
            cut.Find("#price-product").Change("11");

            cut.WaitForAssertion(() =>
            {
                var row = Assert.Single(cut.FindAll("tbody tr"));
                Assert.Contains("900", row.TextContent);
            }, Timeout);
        }

        [Fact]
        public async Task Create_SendsTheProductMonthAndPrice()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Seeded(ctx);

            var cut = ctx.Render<AdminPricesPanel>();
            cut.Find("#price-month").Change("7");
            cut.Find("input").Input("1200");
            cut.Find("#price-save").Click();

            cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("tbody tr").Count), Timeout);
            Assert.Equal("CreatePrice:10|7|1200", Assert.Single(api.Calls));
            Assert.Contains(ctx.Localizer()["AdminSaved"].Value, TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task EmptyOrZeroPrice_IsRejected_WithoutCallingTheApi()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Seeded(ctx);

            var cut = ctx.Render<AdminPricesPanel>();
            cut.Find("#price-save").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["AdminPriceInvalid"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Empty(api.Calls);
        }

        [Fact]
        public async Task Edit_PrefillsTheMonthAndPrice_AndSavesWithTheId()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Seeded(ctx);

            var cut = ctx.Render<AdminPricesPanel>();
            cut.FindAll("[data-action='edit']")[1].Click();

            Assert.Equal("2", cut.Find("#price-month").SelectedValue());
            Assert.Equal("1650", cut.Find("input").GetAttribute("value"));

            cut.Find("input").Input("1600");
            cut.Find("#price-save").Click();

            cut.WaitForAssertion(() => Assert.Contains("1600", cut.Find("tbody").TextContent), Timeout);
            Assert.Equal("ReplacePrice:2:10|2|1600", Assert.Single(api.Calls));
        }

        [Fact]
        public async Task Delete_AfterConfirmation_RemovesTheRow()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
            var api = Seeded(ctx);

            var cut = ctx.Render<AdminPricesPanel>();
            cut.FindAll("[data-action='delete']")[0].Click();

            cut.WaitForAssertion(() => Assert.Single(cut.FindAll("tbody tr")), Timeout);
            Assert.Equal("DeletePrice:1", Assert.Single(api.Calls));
        }

        [Fact]
        public async Task DuplicateMonth_ShowsTheServerReason()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Seeded(ctx);
            api.WriteError = "ErrorConflict";

            var cut = ctx.Render<AdminPricesPanel>();
            cut.Find("input").Input("999");
            cut.Find("#price-save").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["ErrorConflict"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Equal(2, cut.FindAll("tbody tr").Count);
        }

        [Fact]
        public async Task WithoutProducts_AsksToAddOneFirst()
        {
            await using var ctx = TestHelpers.CreateContext();

            var cut = ctx.Render<AdminPricesPanel>();

            Assert.Contains(ctx.Localizer()["AdminAddProductFirst"].Value, TestHelpers.Text(cut.Find(".empty-state").TextContent));
            Assert.Empty(cut.FindAll("#price-save"));
        }

        [Fact]
        public async Task LoadFailure_ShowsTheRealReason()
        {
            await using var ctx = TestHelpers.CreateContext();
            Seeded(ctx).FailReads = true;

            var cut = ctx.Render<AdminPricesPanel>();

            Assert.Contains("HTTP 500", cut.Find(".error-state").TextContent);
        }
    }
}
