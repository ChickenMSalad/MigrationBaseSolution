using Migration.Application.Taxonomy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.Infrastructure.Taxonomy;

public static class TaxonomyServiceCollectionExtensions
{
    public static IServiceCollection AddTaxonomyBuilder(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TaxonomyBuilderOptions>(configuration.GetSection("TaxonomyBuilder"));

        services.AddSingleton<ITaxonomyExcelWriter, TaxonomyExcelWriter>();
        services.AddScoped<TaxonomyExportService>();

        services.AddHttpClient<BynderTaxonomyProvider>();
        services.AddHttpClient<CloudinaryTaxonomyProvider>();
        services.AddHttpClient<AprimoTaxonomyProvider>();

        services.AddScoped<ITaxonomyProvider>(sp => sp.GetRequiredService<BynderTaxonomyProvider>());
        services.AddScoped<ITaxonomyProvider>(sp => sp.GetRequiredService<CloudinaryTaxonomyProvider>());
        services.AddScoped<ITaxonomyProvider>(sp => sp.GetRequiredService<AprimoTaxonomyProvider>());

        return services;
    }
}
