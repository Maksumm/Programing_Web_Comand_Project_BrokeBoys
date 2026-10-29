using System.Net;
using BlazorClient.Services;
using Xunit;

namespace BlazorClient.Tests
{
    public class ListStateErrorTests
    {
        [Fact]
        public async Task FailedLoad_KeepsTheReasonAndStatus()
        {
            var state = new ListState<int>();

            await state.EnsureLoadedAsync(() => throw new HttpRequestException("denied", null, HttpStatusCode.Unauthorized));

            Assert.Equal(ViewState.Error, state.State);
            Assert.Equal("ErrorSignInRequired", state.ErrorKey);
            Assert.Equal(401, state.ErrorStatus);
        }

        [Fact]
        public async Task SuccessfulReload_ClearsTheReason()
        {
            var state = new ListState<int>();
            await state.EnsureLoadedAsync(() => throw new HttpRequestException("down"));
            Assert.Equal("ErrorCannotReachApi", state.ErrorKey);

            await state.ReloadAsync(() => Task.FromResult<IEnumerable<int>>(new[] { 1 }));

            Assert.Equal(ViewState.Success, state.State);
            Assert.Null(state.ErrorKey);
            Assert.Null(state.ErrorStatus);
        }

        [Fact]
        public void SetError_WithoutArguments_StillWorks()
        {
            var state = new ListState<int>();

            state.SetError();

            Assert.Equal(ViewState.Error, state.State);
            Assert.Null(state.ErrorKey);
        }

        [Fact]
        public async Task RefreshFailure_KeepsCurrentItems()
        {
            var state = new ListState<int>();
            state.SetItems(new[] { 7 });

            await state.RefreshAsync(() => throw new HttpRequestException("down"));

            Assert.Equal(ViewState.Success, state.State);
            Assert.Equal(new[] { 7 }, state.Items);
        }
    }
}
