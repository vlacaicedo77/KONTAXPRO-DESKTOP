using Xunit;

namespace KONTAXPRO.Tests.PostgreSql;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (!PostgreSqlTestDatabase.IsConnectionConfigured)
        {
            Skip = "Define KONTAXPRO_TEST_CONNECTION_STRING para ejecutar " +
                "las pruebas PostgreSQL opt-in.";
        }
    }
}
