using Npgsql;

namespace ApiApp.Common;

/// <summary>Розпізнає помилки PostgreSQL за SQLSTATE (вони приходять і напряму, і всередині DbUpdateException).</summary>
internal static class PgErrors
{
    public const string UniqueViolation = "23505";
    public const string ForeignKeyViolation = "23503";

    public static bool Is(Exception exception, string sqlState) =>
        (exception as PostgresException ?? exception.InnerException as PostgresException)?.SqlState == sqlState;
}
