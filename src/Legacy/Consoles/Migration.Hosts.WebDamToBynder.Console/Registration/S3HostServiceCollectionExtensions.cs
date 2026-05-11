using Amazon;
using Amazon.Runtime;
using Amazon.S3;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Migration.Connectors.Sources.S3.Clients;
using Migration.Connectors.Sources.S3.Configuration;

namespace Migration.Hosts.WebDamToBynder.Console.Registration;

public static class S3HostServiceCollectionExtensions
{
    public static IServiceCollection AddS3HostServices(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<S3Options>>().Value;
            var creds = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);

            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
            };

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                config.ServiceURL = options.ServiceUrl;
                config.ForcePathStyle = true;
            }

            return new AmazonS3Client(creds, config);
        });

        services.AddSingleton<IS3Storage, S3Storage>();
        services.AddSingleton<S3Storage>();
        return services;
    }
}
