using BlazorClient.Models;

namespace BlazorClient.Services
{
    public sealed class LiveUpdates : ILiveUpdates
    {
        private readonly IHubClient _hub;
        private readonly HashSet<int> _categories = new();
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _started;
        private bool _connected;

        public LiveUpdates(IHubClient hub)
        {
            _hub = hub;
            _hub.OnPriceChanged(message => PriceChanged?.Invoke(message));
            _hub.OnCatalogueChanged(message => CatalogueChanged?.Invoke(message));
            _hub.Reconnecting += OnReconnecting;
            _hub.Reconnected += RejoinAsync;
        }

        public event Action<PriceChanged>? PriceChanged;

        public event Action<CatalogueChanged>? CatalogueChanged;

        public event Action? StateChanged;

        public bool IsLive => _connected && _categories.Count > 0;

        public async Task JoinCategoryAsync(int categoryId)
        {
            await _gate.WaitAsync();
            try
            {
                var isNew = _categories.Add(categoryId);
                if (!_started)
                {
                    if (!await TryStartAsync())
                    {
                        return;
                    }

                    foreach (var id in _categories.ToArray())
                    {
                        await _hub.JoinAsync(id);
                    }
                }
                else if (isNew)
                {
                    await _hub.JoinAsync(categoryId);
                }
            }
            finally
            {
                _gate.Release();
            }

            StateChanged?.Invoke();
        }

        private async Task<bool> TryStartAsync()
        {
            try
            {
                await _hub.StartAsync();
            }
            catch (Exception)
            {
                return false; // сервер недоступний: наступний виклик спробує ще раз
            }

            _started = true;
            _connected = true;
            return true;
        }

        private Task OnReconnecting()
        {
            _connected = false;
            StateChanged?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>Сервер не пам'ятає групи після розриву: приєднуємося до всіх своїх груп знову.</summary>
        private async Task RejoinAsync()
        {
            await _gate.WaitAsync();
            try
            {
                foreach (var id in _categories.ToArray())
                {
                    await _hub.JoinAsync(id);
                }

                _connected = true;
            }
            finally
            {
                _gate.Release();
            }

            StateChanged?.Invoke();
        }
    }
}
