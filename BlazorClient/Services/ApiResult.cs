namespace BlazorClient.Services
{
    /// <summary>Результат запису на сервер: успіх або ключ локалізованого повідомлення про помилку (ключ у .resx).</summary>
    public sealed record ApiResult(bool Succeeded, string? ErrorKey = null)
    {
        public static ApiResult Ok() => new(true);

        public static ApiResult Fail(string errorKey) => new(false, errorKey);
    }

    public sealed record ApiResult<T>(bool Succeeded, T? Value = default, string? ErrorKey = null)
    {
        public static ApiResult<T> Ok(T value) => new(true, value);

        public static ApiResult<T> Fail(string errorKey) => new(false, default, errorKey);
    }
}
