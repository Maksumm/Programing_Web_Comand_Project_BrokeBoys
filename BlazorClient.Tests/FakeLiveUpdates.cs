using BlazorClient.Models;
using BlazorClient.Services;

namespace BlazorClient.Tests
{
    /// <summary>Підміна «наживо» для тестів сторінок: запам'ятовує групи й дає вручну надіслати повідомлення.</summary>
    internal sealed class FakeLiveUpdates : ILiveUpdates
    {
        public event Action<PriceChanged>? PriceChanged;

        public event Action? StateChanged;

        public bool IsLive { get; private set; }

        public List<int> JoinedCategories { get; } = new();

        public Task JoinCategoryAsync(int categoryId)
        {
            JoinedCategories.Add(categoryId);
            IsLive = true;
            StateChanged?.Invoke();
            return Task.CompletedTask;
        }

        public void Raise(PriceChanged change) => PriceChanged?.Invoke(change);
    }
}
