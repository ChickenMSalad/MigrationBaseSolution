using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Migration.Application.Operational.Telemetry;

public static class OperationalOpenTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddOperationalOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OperationalTelemetryRegistrationOptions>()
            .Bind(configuration.GetSection(OperationalTelemetryRegistrationOptions.SectionName));

        var options = new OperationalTelemetryRegistrationOptions();
        configuration.GetSection(OperationalTelemetryRegistrationOptions.SectionName).Bind(options);

        if (!options.EnableTracing)
        {
            return services;
        }

        var serviceName = string.IsNullOrWhiteSpace(options.ServiceName)
            ? OperationalExecutionActivitySources.Name
            : options.ServiceName.Trim();

        var serviceVersion = OperationalExecutionActivitySources.Version;

        var builder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion))
            .WithTracing(tracing =>
            {
                tracing.AddSource(OperationalExecutionActivitySources.Name);

                if (options.TraceSamplingRatio < 1.0d)
                {
                    tracing.SetSampler(new TraceIdRatioBasedSampler(options.TraceSamplingRatio));
                }

                if (options.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }

                if (options.EnableAzureMonitorExporter)
                {
                    var connectionString = ResolveAzureMonitorConnectionString(configuration, options);
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        tracing.AddAzureMonitorTraceExporter(exporterOptions =>
                        {
                            exporterOptions.ConnectionString = connectionString;
                        });
                    }
                }
            });

        _ = builder;
        return services;
    }

    private static string ResolveAzureMonitorConnectionString(
        IConfiguration configuration,
        OperationalTelemetryRegistrationOptions options)
    {
        return FirstNonEmpty(
            options.AzureMonitorConnectionString,
            configuration[OperationalTelemetryConfigurationKeys.AzureMonitorConnectionString],
            configuration[OperationalTelemetryConfigurationKeys.ApplicationInsightsConnectionString],
            configuration[OperationalTelemetryConfigurationKeys.ApplicationInsightsEnvironmentVariable]);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
