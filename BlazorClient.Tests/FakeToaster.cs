using BlazorClient.Services;

namespace BlazorClient.Tests
{
    /// <summary>Підміна тостів: запам'ятовує тексти, щоб тест міг перевірити, що саме побачив би користувач.</summary>
    internal sealed class FakeToaster : IToastService
    {
        public List<string> Messages { get; } = new();

        public void Info(string message) => Messages.Add(message);
    }
}
