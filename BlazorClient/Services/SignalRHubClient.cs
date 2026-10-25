using BlazorClient.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorClient.Services
{
    public sealed class SignalRHubClient : IHubClient
    {
        private readonly HubConnection _connection;

        public SignalRHubClient(string hubUrl, IAccessTokenSource tokens)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => options.AccessTokenProvider = tokens.GetAsync)
                .WithAutomaticReconnect()
                .Build();

            _connection.Reconnecting += _ => Reconnecting?.Invoke() ?? Task.CompletedTask;
            _connection.Reconnected += _ => Reconnected?.Invoke() ?? Task.CompletedTask;
        }

        public event Func<Task>? Reconnecting;

        public event Func<Task>? Reconnected;

        public Task StartAsync() => _connection.StartAsync();

        public Task JoinAsync(int categoryId) => _connection.InvokeAsync("JoinCategory", categoryId);

        public IDisposable OnPriceChanged(Action<PriceChanged> handler) => _connection.On("PriceChanged", handler);

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}
