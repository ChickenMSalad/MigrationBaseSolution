using Migration.Application.Abstractions;
using Migration.Domain.Models;

namespace Migration.Connectors.Targets.Aprimo;

public sealed class AprimoTargetConnector : IAssetTargetConnector
{
    public string Type => "Aprimo";

    public Task<MigrationResult> UpsertAsync(
        MigrationJobDefinition job,
        AssetWorkItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(item);

        var baseUrl = Resolve(job.Settings, "AprimoBaseUrl", "BaseUrl", "TargetCredential_BaseUrl");
        var clientId = Resolve(job.Settings, "AprimoClientId", "ClientId", "TargetCredential_ClientId");
        var clientSecret = Resolve(job.Settings, "AprimoClientSecret", "ClientSecret", "TargetCredential_ClientSecret");
        var tenant = Resolve(job.Settings, "AprimoTenant", "Tenant", "TargetCredential_Tenant");

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return Task.FromResult(Fail(item, "Aprimo target credentials are missing. Bind an Aprimo target credential set with BaseUrl, ClientId, and ClientSecret."));
        }

        var binaryUri = item.TargetPayload?.Binary?.SourceUri ?? item.SourceAsset?.Binary?.SourceUri ?? item.Manifest.SourcePath;
        if (string.IsNullOrWhiteSpace(binaryUri))
        {
            return Task.FromResult(Fail(item, "Aprimo target connector could not resolve a source binary path/URL for the work item."));
        }

        // This connector is now registered and credential-aware. The API upload implementation is intentionally
        // left behind this connector boundary so the platform can run preflight/dry-run safely before enabling
        // Aprimo asset creation.
        return Task.FromResult(new MigrationResult
        {
            WorkItemId = item.WorkItemId,
            Success = true,
            TargetAssetId = $"aprimo:{item.Manifest.SourceAssetId ?? item.WorkItemId}",
            Message = $"Aprimo connector resolved credentials for {(string.IsNullOrWhiteSpace(tenant) ? "default tenant" : tenant)} and source binary '{binaryUri}'. Upload API implementation is pending."
        });
    }

    private static MigrationResult Fail(AssetWorkItem item, string message) => new()
    {
        WorkItemId = item.WorkItemId,
        Success = false,
        Message = message
    };

    private static string? Resolve(IDictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
