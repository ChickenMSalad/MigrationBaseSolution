using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Migration.Connectors.Sources.WebDam.Clients;
using Migration.Connectors.Sources.WebDam.Configuration;
using Migration.Connectors.Sources.WebDam.Services;

namespace Migration.Hosts.WebDamToBynder.Console.Registration;

public static class WebDamHostServiceCollectionExtensions
{
    public static IServiceCollection AddWebDamHostServices(this IServiceCollection services)
    {
        services.AddSingleton<IWebDamTokenStore, InMemoryWebDamTokenStore>();

        services.AddSingleton<WebDamAuthClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<WebDamOptions>>();
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute)
            };

            return new WebDamAuthClient(
                httpClient,
                options,
                sp.GetRequiredService<IWebDamTokenStore>());
        });

        services.AddSingleton<WebDamApiClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<WebDamOptions>>();
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute)
            };

            return new WebDamApiClient(
                httpClient,
                sp.GetRequiredService<WebDamAuthClient>(),
                options);
        });

        services.AddSingleton<WebDamExportService>();
        services.AddSingleton<WebDamExcelExporter>();

        return services;
    }
}
