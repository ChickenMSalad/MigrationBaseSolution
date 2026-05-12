using Microsoft.Extensions.DependencyInjection;

namespace Migration.Connectors.Targets.Aprimo.Workbooks;

public static class AprimoConfigurationWorkbookServiceCollectionExtensions
{
    public static IServiceCollection AddAprimoConfigurationWorkbookServices(this IServiceCollection services)
    {
        services.AddTransient<HttpClient>();
        services.AddTransient<IAprimoConfigurationWorkbookService, AprimoConfigurationWorkbookService>();

        return services;
    }
}
