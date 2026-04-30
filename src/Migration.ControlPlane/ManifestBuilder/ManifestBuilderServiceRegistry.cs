namespace Migration.ControlPlane.ManifestBuilder;

public sealed class ManifestBuilderServiceRegistry
{
    private readonly IReadOnlyDictionary<string, ISourceManifestService> _services;

    public ManifestBuilderServiceRegistry(IEnumerable<ISourceManifestService> services)
    {
        _services = services.ToDictionary(
            x => BuildKey(x.SourceType, x.ServiceName),
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ManifestBuilderSourceDescriptor> GetSources()
    {
        return _services.Values
            .Select(x => x.GetDescriptor())
            .GroupBy(x => x.SourceType, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var services = group
                    .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new ManifestBuilderSourceDescriptor(
                    group.Key,
                    GetDisplayName(group.Key),
                    services);
            })
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
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

    private static string BuildKey(string sourceType, string serviceName)
        => $"{sourceType.Trim()}::{serviceName.Trim()}";

    private static string GetDisplayName(string sourceType)
    {
        return sourceType.Equals("webdam", StringComparison.OrdinalIgnoreCase)
            ? "WebDam"
            : sourceType;
    }
}
