using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Migration.Admin.Api.Operational.Events;
using Migration.Admin.Api.Operational.Execution;

namespace Migration.Admin.Api.Operational;

public static class OperationalAdminServiceCollectionExtensions
{
    public static IServiceCollection AddOperationalAdminServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IOperationalEventStore, SqlOperationalEventStore>();
        services.AddScoped<IOperationalEventQueryService, SqlOperationalEventQueryService>();

        services.AddScoped<IExecutionSessionStore, SqlExecutionSessionStore>();
        services.AddScoped<IExecutionLifecycleService, SqlExecutionLifecycleService>();
        services.AddScoped<IExecutionPlanStore, SqlExecutionPlanStore>();
        services.AddScoped<IExecutionWorkItemQueueStore, SqlExecutionWorkItemQueueStore>();
        services.AddScoped<IExecutionControlService, SqlExecutionControlService>();
        services.AddScoped<IExecutionWorkerHeartbeatStore, SqlExecutionWorkerHeartbeatStore>();

        services.AddExecutionReplayServices(configuration);

        return services;
    }
}
