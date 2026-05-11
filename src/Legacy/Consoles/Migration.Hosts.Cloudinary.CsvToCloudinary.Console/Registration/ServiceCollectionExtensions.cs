using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Targets.Cloudinary.Clients;
using Migration.Connectors.Targets.Cloudinary.Configuration;
using Migration.Connectors.Targets.Cloudinary.Services;
using Migration.Hosts.Cloudinary.CsvToCloudinary.Console.Infrastructure;
using Migration.Hosts.Cloudinary.CsvToCloudinary.Console.Plugins;
using Migration.Shared.Files;

namespace Migration.Hosts.Cloudinary.CsvToCloudinary.Console.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCloudinaryCsvToCloudinaryHost(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = false;
                options.TimestampFormat = "HH:mm:ss ";
            });
        });

        services.AddSingleton<IConsoleReaderService, ConsoleReaderService>();
        services.AddSingleton<PluginMenuBuilder>();

        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.Configure<CloudinaryCsvMigrationOptions>(configuration.GetSection(CloudinaryCsvMigrationOptions.SectionName));

        services.AddHttpClient<ICloudinaryAdminClient, CloudinaryAdminClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CloudinaryOptions>>().Value;
            client.BaseAddress = new Uri($"https://api.cloudinary.com/v1_1/{options.CloudName.Trim('/')}/");
            client.Timeout = TimeSpan.FromSeconds(Math.Max(30, options.TimeoutSeconds));
        });

        services.AddSingleton<CloudinaryClientFactory>();
        services.AddSingleton<CloudinaryMappingProfileLoader>();
        services.AddSingleton<CloudinaryStructuredMetadataService>();
        services.AddSingleton<CloudinaryUploadService>();
        services.AddSingleton<CloudinaryCsvMigrationService>();

        services.AddScoped<IPlugin, CloudinaryMigrationPlugin>();

        return services;
    }
}
