using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Migration.Application.Operational.Health;

namespace Migration.Infrastructure.Sql.Operational.Health;

public static class SqlOperationalHealthProbeServiceCollectionExtensions
{
    public static IServiceCollection AddSqlOperationalHealthProbes(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IOperationalHealthProbeService, SqlOperationalHealthProbeService>();
        return services;
    }
}
