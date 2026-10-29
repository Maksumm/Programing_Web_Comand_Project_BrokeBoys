using BlazorClient.Components;
using BlazorClient.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests
{
    public class AdminCategoriesPanelTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        private static FakeAdminApi Api(BunitContext ctx) => ctx.Services.GetRequiredService<FakeAdminApi>();

        private static void Fill(IRenderedComponent<AdminCategoriesPanel> cut, string nameUk, string nameEn, string descriptionUk, string descriptionEn)
        {
            cut.FindAll("input")[0].Input(nameUk);
            cut.FindAll("input")[1].Input(nameEn);
            cut.FindAll("input")[2].Input(descriptionUk);
            cut.FindAll("input")[3].Input(descriptionEn);
        }

        [Fact]
        public async Task ListsTheCategories_WithTheirCounts()
        {
            await using var ctx = TestHelpers.CreateContext();
            Api(ctx).Categories.Add(new AdminCategory(1, "Дрова", "Firewood", "Опис", "Description", 2, 7));

            var cut = ctx.Render<AdminCategoriesPanel>();

            var row = Assert.Single(cut.FindAll("tbody tr"));
            Assert.Contains("Дрова", row.TextContent);
            Assert.Contains("Firewood", row.TextContent);
            Assert.Contains("7", row.TextContent);
        }

        [Fact]
        public async Task EmptyList_ShowsTheEmptyMessage_AndKeepsTheFormAvailable()
        {
            await using var ctx = TestHelpers.CreateContext();

            var cut = ctx.Render<AdminCategoriesPanel>();

            Assert.Equal(ctx.Localizer()["AdminNothingYet"].Value, cut.Find(".empty-state").TextContent.Trim());
            Assert.Equal(4, cut.FindAll("input").Count);
        }

        [Fact]
        public async Task Create_SendsTrimmedValues_ConfirmsAndClearsTheForm()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Api(ctx);

            var cut = ctx.Render<AdminCategoriesPanel>();
            Fill(cut, " Цегла ", "Bricks", "Будівельна", "Building");
            cut.Find("#category-save").Click();

            cut.WaitForAssertion(() => Assert.Single(cut.FindAll("tbody tr")), Timeout);
            Assert.Equal("CreateCategory:Цегла|Bricks|Будівельна|Building", Assert.Single(api.Calls));
            Assert.Contains(ctx.Localizer()["AdminSaved"].Value, TestHelpers.Text(cut.Markup));
            Assert.All(cut.FindAll("input"), input => Assert.True(string.IsNullOrEmpty(input.GetAttribute("value"))));
        }

        [Fact]
        public async Task BlankFields_ShowTheRequiredMessage_AndDoNotCallTheApi()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Api(ctx);

            var cut = ctx.Render<AdminCategoriesPanel>();
            Fill(cut, "Цегла", "", "Опис", "Description");
            cut.Find("#category-save").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["AdminFillRequired"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Empty(api.Calls);
        }

        [Fact]
        public async Task Edit_PrefillsTheForm_AndSavesWithTheRecordId()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Api(ctx);
            api.Categories.Add(new AdminCategory(5, "Дрова", "Firewood", "Опис", "Description", 0, 0));

            var cut = ctx.Render<AdminCategoriesPanel>();
            cut.Find("[data-action='edit']").Click();

            Assert.Equal("Дрова", cut.FindAll("input")[0].GetAttribute("value"));
            Assert.NotNull(cut.Find("#category-cancel"));

            cut.FindAll("input")[1].Input("Logs");
            cut.Find("#category-save").Click();

            cut.WaitForAssertion(() => Assert.Contains("Logs", cut.Find("tbody").TextContent), Timeout);
            Assert.Equal("ReplaceCategory:5:Дрова|Logs|Опис|Description", Assert.Single(api.Calls));
            Assert.Empty(cut.FindAll("#category-cancel"));
        }

        [Fact]
        public async Task Cancel_LeavesEditing_WithoutCallingTheApi()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Api(ctx);
            api.Categories.Add(new AdminCategory(5, "Дрова", "Firewood", "Опис", "Description", 0, 0));

            var cut = ctx.Render<AdminCategoriesPanel>();
            cut.Find("[data-action='edit']").Click();
            cut.Find("#category-cancel").Click();

            Assert.Empty(cut.FindAll("#category-cancel"));
            Assert.True(string.IsNullOrEmpty(cut.FindAll("input")[0].GetAttribute("value")));
            Assert.Empty(api.Calls);
        }

        [Fact]
        public async Task Delete_AfterConfirmation_RemovesTheRow()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
            var api = Api(ctx);
            api.Categories.Add(new AdminCategory(5, "Дрова", "Firewood", "Опис", "Description", 0, 0));

            var cut = ctx.Render<AdminCategoriesPanel>();
            cut.Find("[data-action='delete']").Click();

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("tbody tr")), Timeout);
            Assert.Equal("DeleteCategory:5", Assert.Single(api.Calls));
        }

        [Fact]
        public async Task Delete_WhenDeclined_DoesNothing()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<bool>("confirm", _ => true).SetResult(false);
            var api = Api(ctx);
            api.Categories.Add(new AdminCategory(5, "Дрова", "Firewood", "Опис", "Description", 0, 0));

            var cut = ctx.Render<AdminCategoriesPanel>();
            cut.Find("[data-action='delete']").Click();

            Assert.Single(cut.FindAll("tbody tr"));
            Assert.Empty(api.Calls);
        }

        [Fact]
        public async Task Delete_RefusedByTheServer_ShowsTheReason_AndKeepsTheRow()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
            var api = Api(ctx);
            api.Categories.Add(new AdminCategory(5, "Дрова", "Firewood", "Опис", "Description", 2, 0));
            api.WriteError = "ErrorConflict";

            var cut = ctx.Render<AdminCategoriesPanel>();
            cut.Find("[data-action='delete']").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["ErrorConflict"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Single(cut.FindAll("tbody tr"));
        }

        [Fact]
        public async Task LoadFailure_ShowsTheRealReason_AndRetryLoadsTheList()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = Api(ctx);
            api.Categories.Add(new AdminCategory(1, "Дрова", "Firewood", "Опис", "Description", 0, 0));
            api.FailReads = true;

            var cut = ctx.Render<AdminCategoriesPanel>();

            var text = TestHelpers.Text(cut.Find(".error-state").TextContent);
            Assert.Contains(ctx.Localizer()["ErrorServerFailure"].Value, text);
            Assert.Contains("HTTP 500", text);

            api.FailReads = false;
            cut.Find(".error-state button").Click();

            cut.WaitForAssertion(() => Assert.Single(cut.FindAll("tbody tr")), Timeout);
        }
    }
}
