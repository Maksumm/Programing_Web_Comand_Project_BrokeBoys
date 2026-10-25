using ApiApp.Common;
using Microsoft.AspNetCore.SignalR;

namespace ApiApp.Realtime;

/// <summary>
/// Клієнт підписується на групи категорій, які показує (JoinCategory), і отримує повідомлення лише про їхні зміни.
/// Сервер не пам'ятає групи після розриву з'єднання: після перепідключення клієнт приєднується до своїх груп знову.
/// </summary>
public sealed class PricesHub(ILogger<PricesHub> logger) : Hub
{
    public static string GroupName(int categoryId) => $"category-{categoryId}";

    public async Task JoinCategory(int categoryId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(categoryId));
        Log.GroupJoined(logger, Context.ConnectionId, categoryId);
    }

    public async Task LeaveCategory(int categoryId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(categoryId));
        Log.GroupLeft(logger, Context.ConnectionId, categoryId);
    }
}
