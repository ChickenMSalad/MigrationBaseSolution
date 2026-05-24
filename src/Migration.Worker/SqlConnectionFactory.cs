using System.Data.Common;
using System.Reflection;

namespace Migration.Worker;

internal static class SqlConnectionFactory
{
    public static async Task<DbConnection> OpenAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
        string? connectionString = configuration.GetConnectionString("MigrationOperationalStore");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration["MigrationRuntime:SqlOperationalRuntime:ConnectionString"];
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Missing SQL operational store connection string. Configure ConnectionStrings:MigrationOperationalStore or MigrationRuntime:SqlOperationalRuntime:ConnectionString.");
        }

        Type sqlConnectionType = ResolveSqlConnectionType();
        if (Activator.CreateInstance(sqlConnectionType, connectionString) is not DbConnection connection)
        {
            throw new InvalidOperationException("Could not create a Microsoft.Data.SqlClient.SqlConnection instance.");
        }

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static Type ResolveSqlConnectionType()
    {
        const string typeName = "Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient";
        Type? sqlConnectionType = Type.GetType(typeName, throwOnError: false);

        if (sqlConnectionType is not null)
        {
            return sqlConnectionType;
        }

        try
        {
            Assembly assembly = Assembly.Load("Microsoft.Data.SqlClient");
            sqlConnectionType = assembly.GetType("Microsoft.Data.SqlClient.SqlConnection", throwOnError: false);
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            throw new InvalidOperationException(
                "Microsoft.Data.SqlClient is required by the worker host. Add it through centralized package management; do not use an inline PackageReference Version.",
                ex);
        }

        return sqlConnectionType ?? throw new InvalidOperationException(
            "Microsoft.Data.SqlClient.SqlConnection type was not found. Add Microsoft.Data.SqlClient through centralized package management.");
    }
}
