using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Common;

/// <summary>Збирає помилки полів і перетворює їх на 400 із ValidationProblemDetails (ключі — camelCase, як у JSON).</summary>
internal sealed class ErrorBag
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Count > 0;

    public static string CamelCase(string field) =>
        field.Length == 0 ? field : char.ToLowerInvariant(field[0]) + field[1..];

    public ErrorBag Add(string field, string message)
    {
        var key = CamelCase(field);
        if (!_errors.TryGetValue(key, out var list))
        {
            _errors[key] = list = [];
        }

        list.Add(message);
        return this;
    }

    public ErrorBag RequiredText(string field, string? value, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Add(field, $"{CamelCase(field)} is required.");
        }

        return value.Trim().Length > maxLength
            ? Add(field, $"{CamelCase(field)} must be at most {maxLength} characters.")
            : this;
    }

    public ErrorBag OptionalText(string field, string? value, int maxLength = 1000) =>
        value is not null && value.Trim().Length > maxLength
            ? Add(field, $"{CamelCase(field)} must be at most {maxLength} characters.")
            : this;

    public ErrorBag Email(string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Add(field, $"{CamelCase(field)} is required.");
        }

        return new EmailAddressAttribute().IsValid(value.Trim())
            ? this
            : Add(field, $"{CamelCase(field)} is not a valid e-mail address.");
    }

    public ErrorBag RequiredId(string field, int? value) =>
        (value is null or <= 0) ? Add(field, $"{CamelCase(field)} is required.") : this;

    public ErrorBag RequiredBool(string field, bool? value) =>
        (value is null) ? Add(field, $"{CamelCase(field)} is required.") : this;

    public ErrorBag Month(string field, int? value) =>
        (value is null or < 1 or > 12) ? Add(field, $"{CamelCase(field)} must be between 1 and 12.") : this;

    public ErrorBag Price(string field, decimal? value) =>
        (value is null or <= 0 or > 1_000_000m) ? Add(field, $"{CamelCase(field)} must be greater than 0 and at most 1000000.") : this;

    /// <summary>Поріг необов'язковий; якщо є — від 0,01 до 1000 (те саме правило, що й у формі клієнта).</summary>
    public ErrorBag Threshold(string field, decimal? value) =>
        (value is < 0.01m or > 1000m) ? Add(field, $"{CamelCase(field)} must be between 0.01 and 1000.") : this;

    public ValidationProblem ToResult() =>
        TypedResults.ValidationProblem(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
}
