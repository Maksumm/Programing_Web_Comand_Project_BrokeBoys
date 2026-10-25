using BlazorClient.Models;

namespace BlazorClient.Services
{
    /// <summary>
    /// Ролі користувача беруться від API, бо app roles живуть у реєстрації API і потрапляють лише в access-токен
    /// (ID-токен клієнта їх не містить). Відповідь кешується; невдалий запит не кешується, щоб його можна було повторити.
    /// </summary>
    public sealed class CurrentUserState(ICatalogueApi api)
    {
        private Task<UserInfo?>? _request;

        public async Task<UserInfo?> GetAsync()
        {
            var request = _request ??= api.GetCurrentUserAsync();
            var user = await request;
            if (user is null)
            {
                _request = null;
            }

            return user;
        }

        public async Task<bool> IsInRoleAsync(string role)
        {
            var user = await GetAsync();
            return user?.Roles.Contains(role, StringComparer.OrdinalIgnoreCase) == true;
        }
    }
}
