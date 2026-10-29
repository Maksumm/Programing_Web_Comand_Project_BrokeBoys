using BlazorClient.Components;
using BlazorClient.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests
{
    public class AdminProductsPanelTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        private static FakeAdminApi Seeded(BunitContext ctx)
        {
            var api = ctx.Services.GetRequiredService<FakeAdminApi>();
            api.Categories.Add(new AdminCategory(1, "Дрова", "Firewood", "Опис", "Description", 1, 0));
            api.Categories.Add(new AdminCategory(2, "Шини", "Tires", "Опис", "Description", 0, 0));
            api.Products.Add(new AdminProduct(10, 1, "Дуб", "Oak"));
            return api;
        }

        [Fact]
        public async Task ListsProducts_WithTheirCategoryName()
        {
            await using var ctx = TestHelpers.CreateContext();
            Seeded(ctx);

            var cut = ctx.Render<AdminProductsPanel>();

            var row = Assert.Single(cut.FindAll("tbody tr"));
            Assert.Contains("Дрова", row.TextContent);
            Assert.Contains("Дуб", row.TextContent);
            Assert.Contains("Oak", row.TextContent);
        }

        [Fact]
        public async Task CategoryDropdown_OffersEveryCategory()
        {
            await using var ctx = TestHelpers.CreateContext();
            Seeded(ctx);

            var cut = ctx.Render<AdminProductsPanel>();

            var values = cut.FindAll("#product-category option").Select(o => o.GetAttribute("value")).ToList();
            Assert.Equal(new[] { "0", "1", "2" }, values);
        }

        [Fact]
        public async Task Create_SendsTheChosenCategoryAndNames()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Seeded(ctx);

            var cut = ctx.Render<AdminProductsPanel>();
            cut.Find("#product-category").Change("2");
            cut.FindAll("input")[0].Input("Зимові");
            cut.FindAll("input")[1].Input("Winter");
            cut.Find("#product-save").Click();

            cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tbody tr").Count), Timeout);
            Assert.Equal("CreateProduct:2|Зимові|Winter", Assert.Single(api.Calls));
            Assert.Contains(ctx.Localizer()["AdminSaved"].Value, TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task WithoutACategory_ShowsTheRequiredMessage_AndDoesNotCallTheApi()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Seeded(ctx);

            var cut = ctx.Render<AdminProductsPanel>();
            cut.FindAll("input")[0].Input("Зимові");
            cut.FindAll("input")[1].Input("Winter");
            cut.Find("#product-save").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["AdminFillRequired"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Empty(api.Calls);
        }

        [Fact]
        public async Task Edit_PrefillsTheCategoryAndNames_AndSavesWithTheId()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Seeded(ctx);

            var cut = ctx.Render<AdminProductsPanel>();
            cut.Find("[data-action='edit']").Click();

            Assert.Equal("1", cut.Find("#product-category").SelectedValue());
            Assert.Equal("Дуб", cut.FindAll("input")[0].GetAttribute("value"));

            cut.Find("#product-category").Change("2");
            cut.Find("#product-save").Click();

            cut.WaitForAssertion(() => Assert.Contains("Шини", cut.Find("tbody").TextContent), Timeout);
            Assert.Equal("ReplaceProduct:10:2|Дуб|Oak", Assert.Single(api.Calls));
        }

        [Fact]
        public async Task Delete_AfterConfirmation_RemovesTheRow()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
            var api = Seeded(ctx);

            var cut = ctx.Render<AdminProductsPanel>();
            cut.Find("[data-action='delete']").Click();

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("tbody tr")), Timeout);
            Assert.Equal("DeleteProduct:10", Assert.Single(api.Calls));
        }

        [Fact]
        public async Task Create_RefusedByTheServer_ShowsTheReason()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Seeded(ctx);
            api.WriteError = "ErrorConflict";

            var cut = ctx.Render<AdminProductsPanel>();
            cut.Find("#product-category").Change("1");
            cut.FindAll("input")[0].Input("Дуб");
            cut.FindAll("input")[1].Input("Oak");
            cut.Find("#product-save").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["ErrorConflict"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Single(cut.FindAll("tbody tr"));
        }

        [Fact]
        public async Task LoadFailure_ShowsTheRealReason()
        {
            await using var ctx = TestHelpers.CreateContext();
            Seeded(ctx).FailReads = true;

            var cut = ctx.Render<AdminProductsPanel>();

            Assert.Contains("HTTP 500", cut.Find(".error-state").TextContent);
        }
    }
}
