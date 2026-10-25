using BlazorClient.Components;
using BlazorClient.Models;
using BlazorClient.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests
{
    public class CurrentUserStateTests
    {
        [Fact]
        public async Task IsInRole_UsesRolesFromTheServer_AndAsksOnlyOnce()
        {
            var api = new FakeCatalogueApi { CurrentUser = new UserInfo("A", "a@example.com", new[] { "Admin" }) };
            var state = new CurrentUserState(api);

            Assert.True(await state.IsInRoleAsync("Admin"));
            Assert.False(await state.IsInRoleAsync("Subscriber"));
            Assert.Equal(1, api.CurrentUserRequests);
        }

        [Fact]
        public async Task IsInRole_IsFalse_WhenTheServerGivesNoUser()
        {
            var state = new CurrentUserState(new FakeCatalogueApi { CurrentUser = null });

            Assert.False(await state.IsInRoleAsync("Admin"));
        }

        [Fact]
        public async Task RoleGate_ShowsContent_ToUsersWithTheRole()
        {
            await using var ctx = TestHelpers.CreateContext();

            var cut = ctx.Render<RoleGate>(p => p
                .Add(c => c.Role, "Subscriber")
                .AddChildContent("<p id=\"secret\">visible</p>"));

            cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#secret")));
        }

        [Fact]
        public async Task RoleGate_ShowsLocalizedWarning_InsteadOfContent_WithoutTheRole()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.Services.GetRequiredService<FakeCatalogueApi>().CurrentUser = new UserInfo("A", "a@example.com", new[] { "Admin" });

            var cut = ctx.Render<RoleGate>(p => p
                .Add(c => c.Role, "Subscriber")
                .AddChildContent("<p id=\"secret\">visible</p>"));

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("#secret")));
            Assert.Contains(ctx.Localizer()["RoleRequired", "Subscriber"].Value, TestHelpers.Text(cut.Markup));
        }
    }
}
