using Npgsql;

namespace ApiApp.Common;

/// <summary>Розпізнає помилки PostgreSQL за SQLSTATE (вони приходять і напряму, і всередині DbUpdateException).</summary>
internal static class PgErrors
{
    public const string UniqueViolation = "23505";
    public const string ForeignKeyViolation = "23503";

    /// <summary>PostgreSQL повідомляє про порушення правила ON DELETE RESTRICT окремим кодом, а не 23503.</summary>
    public const string RestrictViolation = "23001";

    public static bool Is(Exception exception, string sqlState) =>
        (exception as PostgresException ?? exception.InnerException as PostgresException)?.SqlState == sqlState;

    /// <summary>Рядок посилається на інший (RESTRICT або звичайний зовнішній ключ) — видалити/змінити його не можна.</summary>
    public static bool IsReferenceViolation(Exception exception) =>
        Is(exception, ForeignKeyViolation) || Is(exception, RestrictViolation);
}
