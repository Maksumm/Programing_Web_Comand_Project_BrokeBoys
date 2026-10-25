namespace ApiApp.Common;

/// <summary>Усе логування застосунку — через згенеровані методи [LoggerMessage] з шаблонами повідомлень.</summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Demo data was not seeded. Is PostgreSQL running and are the migrations applied (dotnet ef database update)?")]
    public static partial void DemoDataNotSeeded(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Price change {Kind} for product {ProductId} sent to {GroupCount} group(s)")]
    public static partial void PriceBroadcast(ILogger logger, string kind, int productId, int groupCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Broadcasting a price change for product {ProductId} failed")]
    public static partial void BroadcastFailed(ILogger logger, Exception exception, int productId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} joined the group of category {CategoryId}")]
    public static partial void GroupJoined(ILogger logger, string connectionId, int categoryId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} left the group of category {CategoryId}")]
    public static partial void GroupLeft(ILogger logger, string connectionId, int categoryId);
}
