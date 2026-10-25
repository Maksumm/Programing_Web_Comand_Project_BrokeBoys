using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;

namespace BlazorClient.Services
{
    /// <summary>Звідки SignalR бере access-токен для API.</summary>
    public interface IAccessTokenSource
    {
        Task<string?> GetAsync();
    }

    /// <summary>Токен для API від MSAL (звичайний режим).</summary>
    public sealed class MsalAccessTokenSource(IAccessTokenProvider provider, IConfiguration configuration) : IAccessTokenSource
    {
        public async Task<string?> GetAsync()
        {
            var scope = configuration["ApiScope"] ?? throw new InvalidOperationException("ApiScope is not configured.");
            var result = await provider.RequestAccessToken(new AccessTokenRequestOptions { Scopes = new[] { scope } });
            return result.TryGetToken(out var token) ? token.Value : null;
        }
    }

    /// <summary>Готовий токен із конфігурації (лише режим E2E).</summary>
    public sealed class StaticAccessTokenSource(string token) : IAccessTokenSource
    {
        public Task<string?> GetAsync() => Task.FromResult<string?>(token);
    }
}
