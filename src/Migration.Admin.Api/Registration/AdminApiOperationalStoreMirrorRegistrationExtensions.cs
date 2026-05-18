using Migration.Admin.Api.OperationalStore;

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

        services.AddScoped<IAdminOperationalRunMirrorService, AdminOperationalRunMirrorService>();

        return services;
    }
}
