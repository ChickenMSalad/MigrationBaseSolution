using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Migration.Application.Abstractions;
using Migration.Connectors.Sources.WebDam.Clients;
using Migration.Connectors.Sources.WebDam.Configuration;
using Migration.Connectors.Sources.WebDam.Services;

namespace Migration.Connectors.Sources.WebDam.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebDamSourceConnector(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WebDamOptions>(configuration.GetSection(WebDamOptions.SectionName));
        services.AddSingleton<IWebDamTokenStore, InMemoryWebDamTokenStore>();
        services.AddSingleton<WebDamAuthClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<WebDamOptions>>();
            var httpClient = new HttpClient { BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute) };
            return new WebDamAuthClient(httpClient, options, sp.GetRequiredService<IWebDamTokenStore>());
        });
        services.AddSingleton<WebDamApiClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<WebDamOptions>>();
            var httpClient = new HttpClient { BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute) };
            return new WebDamApiClient(httpClient, sp.GetRequiredService<WebDamAuthClient>(), options);
        });
        services.AddSingleton<WebDamExportService>();
        services.AddSingleton<IAssetSourceConnector, WebDamSourceConnector>();
        return services;
    }
}
