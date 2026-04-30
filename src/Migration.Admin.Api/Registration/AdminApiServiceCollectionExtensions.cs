using Migration.Admin.Api.Options;
using Migration.Admin.Api.Services;

namespace Migration.Admin.Api.Registration;

public static class AdminApiServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationAdminApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminApiOptions>(configuration.GetSection(AdminApiOptions.SectionName));
        services.AddSingleton<IAdminProjectStore, FileBackedAdminProjectStore>();
        services.AddSingleton<AdminRunFactory>();
        return services;
    }
}
