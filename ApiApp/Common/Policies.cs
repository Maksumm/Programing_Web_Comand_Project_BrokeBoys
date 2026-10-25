namespace ApiApp.Common;

/// <summary>Назви політик доступу, scope і ролей застосунку (ролі створюються в реєстрації API в Entra ID як app roles).</summary>
public static class Policies
{
    /// <summary>Делегований scope, який клієнт запитує для API.</summary>
    public const string Scope = "access_as_user";

    public const string AdminRole = "Admin";
    public const string SubscriberRole = "Subscriber";

    /// <summary>Читання каталогу (категорії, товари, ціни): достатньо scope.</summary>
    public const string Read = "Read";

    /// <summary>Зміна каталогу: scope і роль Admin.</summary>
    public const string ManageCatalogue = "ManageCatalogue";

    /// <summary>Усі операції з підписками: scope і роль Subscriber.</summary>
    public const string ManageSubscriptions = "ManageSubscriptions";
}
