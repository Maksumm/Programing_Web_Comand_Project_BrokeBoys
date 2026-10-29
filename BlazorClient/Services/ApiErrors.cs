using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BlazorClient.Services
{
    /// <summary>
    /// Перетворює виняток читання на ключ повідомлення (.resx) і код HTTP, щоб екран показав справжню причину збою,
    /// а не завжди «перевірте інтернет».
    /// </summary>
    public static class ApiErrors
    {
        public sealed record Failure(string Key, int? Status);

        public static Failure Describe(Exception exception) => exception switch
        {
            AccessTokenNotAvailableException => new Failure("ErrorSignInRequired", null),
            HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => new Failure("ErrorSignInRequired", 401),
            HttpRequestException { StatusCode: HttpStatusCode.Forbidden } => new Failure("ErrorForbidden", 403),
            HttpRequestException { StatusCode: { } code } => new Failure("ErrorServerFailure", (int)code),

            // Немає коду відповіді: API не запущено, неправильна адреса або браузер заблокував запит (CORS).
            HttpRequestException => new Failure("ErrorCannotReachApi", null),

            // Прийшла не JSON-відповідь (наприклад, index.html клієнта, якщо ApiBaseUrl вказує на сам клієнт).
            JsonException or NotSupportedException => new Failure("ErrorUnexpectedResponse", null),

            _ => new Failure("ErrorServerFailure", null)
        };
    }
}
