
namespace ZY.Infrastructure.Persistence.PostgreSQL;

public static class PostgreSqlConfiguration
{
    private const string ConnectionStringEnvironmentVariable =
        "ZY_POSTGRES_CONNECTION_STRING";

    public static string ConnectionString
    {
        get
        {
            var connectionString = Environment.GetEnvironmentVariable(
                ConnectionStringEnvironmentVariable
            );

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "A variável de ambiente ZY_POSTGRES_CONNECTION_STRING não foi configurada."
                );
            }

            return connectionString;
        }
    }
}
