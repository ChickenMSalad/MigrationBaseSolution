using Migration.ControlPlane.Services;
using Migration.Domain.Models;

namespace Migration.Workers.QueueExecutor.Services;

public sealed class ProjectCredentialJobSettingsHydrator
{
    private readonly ICredentialResolver _credentialResolver;

    public ProjectCredentialJobSettingsHydrator(ICredentialResolver credentialResolver)
    {
        _credentialResolver = credentialResolver;
    }

    public async Task<MigrationJobDefinition> HydrateAsync(
        MigrationJobDefinition job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var settings = new Dictionary<string, string>(job.Settings, StringComparer.OrdinalIgnoreCase);

        await HydrateRoleAsync(
            settings,
            role: "Source",
            connectorType: job.SourceType,
            credentialSetIdSettingNames: new[] { "sourceCredentialSetId", "SourceCredentialSetId" },
            cancellationToken).ConfigureAwait(false);

        await HydrateRoleAsync(
            settings,
            role: "Target",
            connectorType: job.TargetType,
            credentialSetIdSettingNames: new[] { "targetCredentialSetId", "TargetCredentialSetId" },
            cancellationToken).ConfigureAwait(false);

        return new MigrationJobDefinition
        {
            JobName = job.JobName,
            SourceType = job.SourceType,
            TargetType = job.TargetType,
            ManifestType = job.ManifestType,
            MappingProfilePath = job.MappingProfilePath,
            ManifestPath = job.ManifestPath,
            ConnectionString = job.ConnectionString,
            QueryText = job.QueryText,
            Settings = settings,
            DryRun = job.DryRun,
            Parallelism = job.Parallelism
        };
    }

    private async Task HydrateRoleAsync(
        IDictionary<string, string> settings,
        string role,
        string connectorType,
        IReadOnlyCollection<string> credentialSetIdSettingNames,
        CancellationToken cancellationToken)
    {
        var credentialSetId = FirstSetting(settings, credentialSetIdSettingNames);
        if (string.IsNullOrWhiteSpace(credentialSetId))
        {
            return;
        }

        var values = await _credentialResolver.ResolveAsync(credentialSetId, cancellationToken).ConfigureAwait(false);

        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            var normalizedKey = NormalizeSettingKey(pair.Key);

            // Generic role-prefixed settings. These are useful for future connector factories.
            SetIfMissing(settings, $"{role}Credential_{normalizedKey}", pair.Value);
            SetIfMissing(settings, $"{role}{normalizedKey}", pair.Value);

            // Connector-prefixed settings. Existing connectors can read these without depending on the control plane.
            var connectorPrefix = NormalizeSettingKey(connectorType);
            SetIfMissing(settings, $"{connectorPrefix}{normalizedKey}", pair.Value);
        }

        ApplyConnectorAliases(settings, role, connectorType);
    }

    private static void ApplyConnectorAliases(IDictionary<string, string> settings, string role, string connectorType)
    {
        if (connectorType.Equals("WebDam", StringComparison.OrdinalIgnoreCase))
        {
            Alias(settings, "SourceCredential_BaseUrl", "WebDamBaseUrl");
            Alias(settings, "SourceCredential_ClientId", "WebDamClientId");
            Alias(settings, "SourceCredential_ClientSecret", "WebDamClientSecret");
            Alias(settings, "SourceCredential_RefreshToken", "WebDamRefreshToken");
            Alias(settings, "SourceCredential_AccessToken", "WebDamAccessToken");
            Alias(settings, "SourceCredential_RedirectUri", "WebDamRedirectUri");
            Alias(settings, "SourceCredential_Username", "WebDamUsername");
            Alias(settings, "SourceCredential_Password", "WebDamPassword");
        }

        if (connectorType.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            Alias(settings, "TargetCredential_ConnectionString", "AzureBlobTargetConnectionString");
            Alias(settings, "TargetCredential_ContainerName", "AzureBlobTargetContainer");
            Alias(settings, "TargetCredential_Container", "AzureBlobTargetContainer");
            Alias(settings, "TargetCredential_RootFolderPath", "AzureBlobTargetRootFolder");
            Alias(settings, "TargetCredential_FolderPath", "AzureBlobTargetRootFolder");
        }
    }

    private static string? FirstSetting(IDictionary<string, string> settings, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (settings.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static void SetIfMissing(IDictionary<string, string> settings, string key, string value)
    {
        if (!settings.ContainsKey(key) && !string.IsNullOrWhiteSpace(value))
        {
            settings[key] = value;
        }
    }

    private static void Alias(IDictionary<string, string> settings, string from, string to)
    {
        if (settings.TryGetValue(from, out var value) &&
            !string.IsNullOrWhiteSpace(value) &&
            !settings.ContainsKey(to))
        {
            settings[to] = value;
        }
    }

    private static string NormalizeSettingKey(string value)
    {
        var parts = value
            .Split(new[] { '-', '_', ' ', '.', ':' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (parts.Length == 0)
        {
            return string.Empty;
        }

        return string.Concat(parts.Select(ToPascal));
    }

    private static string ToPascal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length == 1)
        {
            return value.ToUpperInvariant();
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}
