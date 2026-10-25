namespace BlazorClient.Models
{
    /// <summary>Поточний користувач за даними API (/api/me): ім'я, пошта й ролі з access-токена.</summary>
    public sealed record UserInfo(string? Name, string? Email, string[] Roles);
}
