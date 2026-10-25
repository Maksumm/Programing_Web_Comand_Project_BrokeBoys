using BlazorClient.Models;
using BlazorClient.Services;
using Xunit;

namespace BlazorClient.Tests
{
    public class LiveUpdatesTests
    {
        private sealed class FakeHubClient : IHubClient
        {
            private Action<PriceChanged>? _handler;

            public int Starts { get; private set; }

            public bool FailStart { get; set; }

            public List<int> Joined { get; } = new();

            public event Func<Task>? Reconnecting;

            public event Func<Task>? Reconnected;

            public Task StartAsync()
            {
                Starts++;
                return FailStart ? Task.FromException(new HttpRequestException("server is down")) : Task.CompletedTask;
            }

            public Task JoinAsync(int categoryId)
            {
                Joined.Add(categoryId);
                return Task.CompletedTask;
            }

            public IDisposable OnPriceChanged(Action<PriceChanged> handler)
            {
                _handler = handler;
                return new Noop();
            }

            public void Receive(PriceChanged message) => _handler?.Invoke(message);

            public Task DropConnectionAsync() => Reconnecting?.Invoke() ?? Task.CompletedTask;

            public Task RestoreConnectionAsync() => Reconnected?.Invoke() ?? Task.CompletedTask;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            private sealed class Noop : IDisposable
            {
                public void Dispose() { }
            }
        }

        [Fact]
        public async Task Join_StartsTheConnectionOnce_AndJoinsEachCategoryOnce()
        {
            var hub = new FakeHubClient();
            var live = new LiveUpdates(hub);

            await live.JoinCategoryAsync(1);
            await live.JoinCategoryAsync(2);
            await live.JoinCategoryAsync(1);

            Assert.Equal(1, hub.Starts);
            Assert.Equal(new[] { 1, 2 }, hub.Joined);
            Assert.True(live.IsLive);
        }

        [Fact]
        public async Task AfterReconnect_TheClientJoinsAllItsGroupsAgain()
        {
            var hub = new FakeHubClient();
            var live = new LiveUpdates(hub);
            await live.JoinCategoryAsync(1);
            await live.JoinCategoryAsync(2);

            await hub.DropConnectionAsync();
            Assert.False(live.IsLive);

            await hub.RestoreConnectionAsync();

            Assert.Equal(new[] { 1, 2, 1, 2 }, hub.Joined);
            Assert.True(live.IsLive);
        }

        [Fact]
        public async Task StateChanged_IsRaised_WhenTheConnectionDropsAndReturns()
        {
            var hub = new FakeHubClient();
            var live = new LiveUpdates(hub);
            await live.JoinCategoryAsync(1);
            var raised = 0;
            live.StateChanged += () => raised++;

            await hub.DropConnectionAsync();
            await hub.RestoreConnectionAsync();

            Assert.Equal(2, raised);
        }

        [Fact]
        public void MessagesFromTheServer_AreForwardedToSubscribers()
        {
            var hub = new FakeHubClient();
            var live = new LiveUpdates(hub);
            PriceChanged? received = null;
            live.PriceChanged += message => received = message;

            hub.Receive(new PriceChanged("Updated", 1, 2, 7, 4242m));

            Assert.Equal(new PriceChanged("Updated", 1, 2, 7, 4242m), received);
        }

        [Fact]
        public async Task WhenTheServerIsDown_JoinDoesNotThrow_AndRetriesNextTime()
        {
            var hub = new FakeHubClient { FailStart = true };
            var live = new LiveUpdates(hub);

            await live.JoinCategoryAsync(1);

            Assert.False(live.IsLive);
            Assert.Empty(hub.Joined);

            hub.FailStart = false;
            await live.JoinCategoryAsync(1);

            Assert.Equal(2, hub.Starts);
            Assert.Equal(new[] { 1 }, hub.Joined);
            Assert.True(live.IsLive);
        }
    }
}
