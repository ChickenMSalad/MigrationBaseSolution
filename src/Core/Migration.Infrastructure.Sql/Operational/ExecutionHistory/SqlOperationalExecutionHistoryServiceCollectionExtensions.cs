using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Application.Operational.ExecutionHistory;

namespace Migration.Infrastructure.Sql.Operational.ExecutionHistory;

public static class SqlOperationalExecutionHistoryServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalExecutionHistory(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SqlOperationalExecutionHistoryOptions>(
            configuration.GetSection("SqlOperationalExecutionHistory"));

        services.AddSingleton<IOperationalExecutionHistoryWriter, SqlOperationalExecutionHistoryWriter>();

        return services;
    }
}
