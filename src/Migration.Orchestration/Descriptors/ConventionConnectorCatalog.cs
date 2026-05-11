using Migration.Application.Abstractions;
using Migration.Orchestration.Abstractions;

namespace Migration.Orchestration.Descriptors;

public sealed class ConventionConnectorCatalog : IConnectorCatalog
{
    private readonly IEnumerable<IAssetSourceConnector> _sources;
    private readonly IEnumerable<IAssetTargetConnector> _targets;
    private readonly IEnumerable<IManifestProvider> _manifestProviders;

    public ConventionConnectorCatalog(
        IEnumerable<IAssetSourceConnector> sources,
        IEnumerable<IAssetTargetConnector> targets,
        IEnumerable<IManifestProvider> manifestProviders)
    {
        _sources = sources;
        _targets = targets;
        _manifestProviders = manifestProviders;
    }

    public IReadOnlyList<ConnectorDescriptor> GetSources() => _sources
        .Select(source => new ConnectorDescriptor
        {
            Type = source.Type,
            DisplayName = ToDisplayName(source.Type),
            Direction = ConnectorDirections.Source,
            Description = $"{ToDisplayName(source.Type)} source connector.",
            Capabilities = { ConnectorCapabilities.ReadAsset }
        })
        .OrderBy(x => x.DisplayName)
        .ToList();

    public IReadOnlyList<ConnectorDescriptor> GetTargets() => _targets
        .Select(target => new ConnectorDescriptor
        {
            Type = target.Type,
            DisplayName = ToDisplayName(target.Type),
            Direction = ConnectorDirections.Target,
            Description = $"{ToDisplayName(target.Type)} target connector.",
            Capabilities = { ConnectorCapabilities.UpsertAsset, ConnectorCapabilities.DryRun }
        })
        .OrderBy(x => x.DisplayName)
        .ToList();

    public IReadOnlyList<ManifestProviderDescriptor> GetManifestProviders() => _manifestProviders
        .Select(provider => new ManifestProviderDescriptor
        {
            Type = provider.Type,
            DisplayName = ToDisplayName(provider.Type),
            Options =
            {
                new ConnectorOptionDescriptor
                {
                    Name = "ManifestPath",
                    DisplayName = "Manifest path",
                    Description = "Path, connection string, or query source used by this manifest provider depending on provider type.",
                    Required = provider.Type.Equals("csv", StringComparison.OrdinalIgnoreCase) || provider.Type.Equals("excel", StringComparison.OrdinalIgnoreCase)
                }
            }
        })
        .OrderBy(x => x.DisplayName)
        .ToList();

    private static string ToDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var chars = new List<char> { char.ToUpperInvariant(value[0]) };
        for (var i = 1; i < value.Length; i++)
        {
            if (char.IsUpper(value[i]) && !char.IsWhiteSpace(value[i - 1]))
            {
                chars.Add(' ');
            }
            chars.Add(value[i]);
        }
        return new string(chars.ToArray());
    }
}
