using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace ApiApp.Common;

/// <summary>Перевірка access-токенів Microsoft Entra ID та політики доступу за scope і ролями.</summary>
internal static class EntraAuthentication
{
    public static IServiceCollection AddEntraAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Налаштування читаються з конфігурації під час першого використання (AzureAd:TenantId, AzureAd:ClientId).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                var instance = (configuration["AzureAd:Instance"] ?? "https://login.microsoftonline.com/").TrimEnd('/');
                var tenantId = configuration["AzureAd:TenantId"];
                var clientId = configuration["AzureAd:ClientId"];

                options.Authority = $"{instance}/{tenantId}/v2.0";

                // Імена claims лишаються такими, як у токені (scp, roles, name…), без перейменування на довгі URI.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    // Токени v2 і v1 мають різних емітентів; ключі підпису беруться з метаданих Authority.
                    ValidIssuers = [$"{instance}/{tenantId}/v2.0", $"https://sts.windows.net/{tenantId}/"],
                    ValidateAudience = true,
                    // Токен має бути виданий саме для цього API (GUID або Application ID URI).
                    ValidAudiences = new[] { clientId, $"api://{clientId}" }.Where(a => !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(a)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "roles"
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Read, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(HasScope))
            .AddPolicy(Policies.ManageCatalogue, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(HasScope)
                .RequireRole(Policies.AdminRole))
            .AddPolicy(Policies.ManageSubscriptions, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(HasScope)
                .RequireRole(Policies.SubscriberRole));

        return services;
    }

    /// <summary>Claim scp містить scope через пробіл ("access_as_user other"); токен застосунку без користувача scp не має.</summary>
    private static bool HasScope(AuthorizationHandlerContext context) =>
        context.User.FindAll("scp")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(Policies.Scope);
}
