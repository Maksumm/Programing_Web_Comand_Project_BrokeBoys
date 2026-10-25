using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;

namespace BlazorClient.Services
{
    /// <summary>Додає access-токен лише до запитів на адресу API (<c>ApiBaseUrl</c>), із scope <c>ApiScope</c>; до інших адрес токен не потрапляє.</summary>
    public sealed class ApiAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        public ApiAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation, IConfiguration configuration)
            : base(provider, navigation)
        {
            var apiBaseUrl = configuration["ApiBaseUrl"]
                ?? throw new InvalidOperationException("ApiBaseUrl is not configured in wwwroot/appsettings.json.");
            var apiScope = configuration["ApiScope"]
                ?? throw new InvalidOperationException("ApiScope is not configured in wwwroot/appsettings.json.");

            ConfigureHandler(authorizedUrls: new[] { apiBaseUrl }, scopes: new[] { apiScope });
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (AccessTokenNotAvailableException exception)
            {
                // Тихо отримати токен не вдалося (сеанс завершився): відправляємо користувача входити знову.
                exception.Redirect();
                throw;
            }
        }
    }
}
