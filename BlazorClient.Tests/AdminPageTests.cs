using BlazorClient.Components;
using BlazorClient.Models;
using BlazorClient.Pages;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests
{
    public class AdminPageTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        private static void SignInAs(BunitContext ctx, params string[] roles) =>
            ctx.Services.GetRequiredService<FakeCatalogueApi>().CurrentUser = new UserInfo("Test", "t@example.com", roles);

        [Fact]
        public async Task WithoutTheAdminRole_ShowsTheRoleRequiredMessage_AndNoAdminForms()
        {
            await using var ctx = TestHelpers.CreateContext();
            SignInAs(ctx, "Subscriber");

            var cut = ctx.Render<AdminPage>();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["RoleRequired", "Admin"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Empty(cut.FindAll("#category-save"));
        }

        [Fact]
        public async Task AdminSeesTheTitle_AndTheCategoriesTabFirst()
        {
            await using var ctx = TestHelpers.CreateContext();
            SignInAs(ctx, "Admin");

            var cut = ctx.Render<AdminPage>();

            cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#category-save")), Timeout);
            Assert.Contains(ctx.Localizer()["AdminTitle"].Value, TestHelpers.Text(cut.Markup));
            Assert.Contains("active", cut.Find("[data-tab='categories']").ClassName);
        }

        [Fact]
        public async Task Tabs_SwitchBetweenTheThreePanels()
        {
            await using var ctx = TestHelpers.CreateContext();
            SignInAs(ctx, "Admin");
            ctx.Services.GetRequiredService<FakeAdminApi>().Products.Add(new AdminProduct(10, 1, "Дуб", "Oak"));

            var cut = ctx.Render<AdminPage>();
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#category-save")), Timeout);

            cut.Find("[data-tab='products']").Click();
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#product-save")), Timeout);
            Assert.Empty(cut.FindAll("#category-save"));

            cut.Find("[data-tab='prices']").Click();
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#price-save")), Timeout);
            Assert.Contains("active", cut.Find("[data-tab='prices']").ClassName);
        }

        [Fact]
        public async Task NavLink_IsShownOnlyToAdmins()
        {
            await using (var ctx = TestHelpers.CreateContext())
            {
                SignInAs(ctx, "Admin", "Subscriber");

                var cut = ctx.Render<AdminNavLink>();

                cut.WaitForAssertion(() => Assert.Equal("admin", cut.Find("a").GetAttribute("href")?.TrimStart('/')), Timeout);
            }

            await using (var ctx = TestHelpers.CreateContext())
            {
                SignInAs(ctx, "Subscriber");

                var cut = ctx.Render<AdminNavLink>();

                Assert.Empty(cut.FindAll("a"));
            }
        }
    }
}
