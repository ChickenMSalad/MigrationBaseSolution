using Migration.Admin.Api.OperationalStore;
using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.Registration;

public static class AdminApiOperationalStoreMirrorRegistrationExtensions
{
    public static IServiceCollection AddMigrationAdminApiOperationalRunMirror(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<OperationalRunMirrorOptions>(
            configuration.GetSection(OperationalRunMirrorOptions.SectionName));

        services.AddSingleton<IValidateOptions<OperationalRunMirrorOptions>, OperationalRunMirrorOptionsValidator>();
        services.AddSingleton<OperationalMirrorInvocationState>();

        services.AddScoped<IAdminOperationalRunMirrorService, AdminOperationalRunMirrorService>();
        services.AddScoped<IOperationalMirrorReadinessEvaluator, OperationalMirrorReadinessEvaluator>();
        services.AddScoped<IOperationalSqlSchemaSmokeTestService, OperationalSqlSchemaSmokeTestService>();
        services.AddScoped<IOperationalMirrorEnablementGuard, OperationalMirrorEnablementGuard>();
        services.AddScoped<IOperationalMirrorWriteVerificationService, OperationalMirrorWriteVerificationService>();
        services.AddScoped<IOperationalMirrorReadService, OperationalMirrorReadService>();

        return services;
    }
}
