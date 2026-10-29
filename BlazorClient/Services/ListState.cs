namespace BlazorClient.Services
{
    public enum ViewState
    {
        Loading,
        Error,
        Empty,
        Success
    }

    /// <summary>
    /// Scoped-сервіс зі станом екрана: стан завантаження та список елементів.
    /// Сторінки підписуються на <see cref="Changed"/> і перерендерюються після кожної зміни.
    /// </summary>
    public class ListState<T>
    {
        private bool _initialized;

        public ViewState State { get; private set; } = ViewState.Loading;

        public IReadOnlyList<T> Items { get; private set; } = Array.Empty<T>();

        /// <summary>Ключ .resx з причиною останньої помилки (<c>null</c>, якщо причина невідома).</summary>
        public string? ErrorKey { get; private set; }

        /// <summary>Код HTTP останньої помилки, якщо сервер відповів.</summary>
        public int? ErrorStatus { get; private set; }

        public event Action? Changed;

        public void SetLoading()
        {
            _initialized = true;
            Items = Array.Empty<T>();
            ErrorKey = null;
            ErrorStatus = null;
            State = ViewState.Loading;
            Changed?.Invoke();
        }

        public void SetError(string? errorKey = null, int? status = null)
        {
            _initialized = true;
            Items = Array.Empty<T>();
            ErrorKey = errorKey;
            ErrorStatus = status;
            State = ViewState.Error;
            Changed?.Invoke();
        }

        public void SetItems(IEnumerable<T> items)
        {
            _initialized = true;
            var list = items.ToList();
            Items = list;
            ErrorKey = null;
            ErrorStatus = null;
            State = list.Count == 0 ? ViewState.Empty : ViewState.Success;
            Changed?.Invoke();
        }

        public void Add(T item) => SetItems(Items.Append(item));

        /// <summary>Повідомляє підписників, що елемент усередині списку змінився.</summary>
        public void NotifyChanged() => Changed?.Invoke();

        /// <summary>Завантажує дані один раз; якщо стан уже задано, нічого не робить.</summary>
        public async Task EnsureLoadedAsync(Func<Task<IEnumerable<T>>> loader)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            await LoadAsync(loader);
        }

        /// <summary>Повторне завантаження (кнопка «Спробувати ще раз»).</summary>
        public async Task ReloadAsync(Func<Task<IEnumerable<T>>> loader)
        {
            SetLoading();
            await LoadAsync(loader);
        }

        /// <summary>
        /// Тихе оновлення (повідомлення «наживо»): без стану «завантаження». Якщо сервер не відповів, лишаються поточні дані.
        /// </summary>
        public async Task RefreshAsync(Func<Task<IEnumerable<T>>> loader)
        {
            try
            {
                SetItems(await loader());
            }
            catch
            {
                // лишаємо те, що вже показано
            }
        }

        private async Task LoadAsync(Func<Task<IEnumerable<T>>> loader)
        {
            try
            {
                SetItems(await loader());
            }
            catch (Exception exception)
            {
                var failure = ApiErrors.Describe(exception);
                SetError(failure.Key, failure.Status);
            }
        }
    }
}
