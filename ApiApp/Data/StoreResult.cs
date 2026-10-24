namespace ApiApp.Data;

public enum StoreStatus
{
    Ok,
    NotFound,
    Conflict,
    InvalidReference
}

/// <summary>Результат операції сховища: endpoint перетворює його на точний HTTP-код.</summary>
public sealed record StoreResult<T>(StoreStatus Status, T? Value = default, string? Field = null, string? Detail = null)
{
    public static StoreResult<T> Ok(T value) => new(StoreStatus.Ok, value);

    public static StoreResult<T> NotFound(string detail) => new(StoreStatus.NotFound, Detail: detail);

    public static StoreResult<T> Conflict(string detail) => new(StoreStatus.Conflict, Detail: detail);

    public static StoreResult<T> InvalidReference(string field, string detail) =>
        new(StoreStatus.InvalidReference, Field: field, Detail: detail);
}
