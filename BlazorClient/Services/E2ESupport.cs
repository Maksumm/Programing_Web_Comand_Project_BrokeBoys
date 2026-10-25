using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorClient.Services
{
    // Режим E2E вмикається лише наявністю "E2E:Token" у wwwroot/appsettings.json (його пише тест). У бойовій конфігурації
    // цього ключа немає. Безпеку не послаблює: API однаково перевіряє токен.

    /// <summary>Завжди «увійшов» як тестовий користувач, щоб наскрізний тест не залежав від входу Microsoft.</summary>
    public sealed class E2EAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "E2E User") }, "e2e");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }

    /// <summary>Додає готовий токен до кожного запиту.</summary>
    public sealed class StaticBearerHandler(string token) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
