using BlazorClient.Services;
using Xunit;

namespace BlazorClient.Tests
{
    public class ListStateRefreshTests
    {
        [Fact]
        public async Task Refresh_ReplacesItems_WithoutPassingThroughLoading()
        {
            var state = new ListState<int>();
            state.SetItems(new[] { 1 });
            var states = new List<ViewState>();
            state.Changed += () => states.Add(state.State);

            await state.RefreshAsync(() => Task.FromResult<IEnumerable<int>>(new[] { 2, 3 }));

            Assert.Equal(new[] { 2, 3 }, state.Items);
            Assert.DoesNotContain(ViewState.Loading, states);
        }

        [Fact]
        public async Task Refresh_KeepsTheCurrentData_WhenTheServerFails()
        {
            var state = new ListState<int>();
            state.SetItems(new[] { 1 });

            await state.RefreshAsync(() => Task.FromException<IEnumerable<int>>(new HttpRequestException("down")));

            Assert.Equal(ViewState.Success, state.State);
            Assert.Equal(new[] { 1 }, state.Items);
        }
    }
}
