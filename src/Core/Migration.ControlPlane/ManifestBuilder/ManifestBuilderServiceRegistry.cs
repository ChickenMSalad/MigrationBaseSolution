using System;
using System.Collections.Generic;
using System.Linq;

namespace Migration.ControlPlane.ManifestBuilder;

public sealed class ManifestBuilderServiceRegistry
{
    private readonly IReadOnlyDictionary<string, ISourceManifestService> _services;

    public ManifestBuilderServiceRegistry(IEnumerable<ISourceManifestService> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services
            .Where(service => service is not null)
            .GroupBy(
                service => BuildKey(service.SourceType, service.ServiceName),
                StringComparer.OrdinalIgnoreCase)
            .Select(SelectService)
            .ToDictionary(
                service => BuildKey(service.SourceType, service.ServiceName),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ManifestBuilderSourceDescriptor> GetSources()
    {
        return _services.Values
            .Select(service => service.GetDescriptor())
            .GroupBy(descriptor => descriptor.SourceType, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var services = group
                    .OrderBy(descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new ManifestBuilderSourceDescriptor(
                    group.Key,
                    GetDisplayName(group.Key),
                    services);
            })
            .OrderBy(source => source.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ISourceManifestService? TryGetService(string sourceType, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(sourceType) || string.IsNullOrWhiteSpace(serviceName))
        {
            return null;
        }

        return _services.TryGetValue(BuildKey(sourceType, serviceName), out var service)
            ? service
            : null;
    }

    private static ISourceManifestService SelectService(IGrouping<string, ISourceManifestService> group)
    {
        var services = group.ToArray();
        var concreteTypes = services
            .Select(service => service.GetType())
            .Distinct()
            .ToArray();

        if (concreteTypes.Length > 1)
        {
            var typeNames = string.Join(", ", concreteTypes.Select(type => type.FullName));
            throw new InvalidOperationException(
                $"Multiple different manifest builder services are registered for key '{group.Key}': {typeNames}.");
        }

        return services[0];
    }

    private static string BuildKey(string sourceType, string serviceName) =>
        $"{sourceType.Trim()}::{serviceName.Trim()}";

    private static string GetDisplayName(string sourceType)
    {
        if (sourceType.Equals("webdam", StringComparison.OrdinalIgnoreCase))
        {
            return "WebDam";
        }

        if (sourceType.Equals("aem", StringComparison.OrdinalIgnoreCase))
        {
            return "AEM";
        }

        return sourceType;
    }
}
