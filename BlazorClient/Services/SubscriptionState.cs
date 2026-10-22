using BlazorClient.Models;

namespace BlazorClient.Services;

/// <summary>
/// Стан підписок користувача. Реєструється як scoped-сервіс (у Blazor WebAssembly
/// scope живе стільки, скільки відкрита вкладка), тож стан спільний для всіх компонентів.
/// </summary>
public class SubscriptionState
{
    private readonly List<Subscription> _items = new();

    public IReadOnlyList<Subscription> Items => _items;

    /// <summary>Викликається після кожної зміни списку. Компоненти підписуються й викликають StateHasChanged.</summary>
    public event Action? Changed;

    public void Add(string email, string categoryName, decimal? threshold = null)
    {
        var id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1;
        _items.Add(new Subscription
        {
            Id = id,
            Email = email,
            CategoryName = categoryName,
            Threshold = threshold
        });
        Changed?.Invoke();
    }

    public void Remove(int id)
    {
        if (_items.RemoveAll(x => x.Id == id) > 0)
        {
            Changed?.Invoke();
        }
    }
}
