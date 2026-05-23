namespace MigrationBase.Core.Cloud.Azure.Infrastructure.Identity;

public sealed class AzureCredentialBoundaryRegistry : IAzureCredentialBoundaryRegistry
{
    private readonly List<AzureCredentialBoundary> _boundaries = new();

    public AzureCredentialBoundaryRegistry()
    {
        Add(new AzureCredentialBoundary
        {
            Name = "sql-operational-store",
            Description = "Credential boundary for SQL-first durable operational execution state.",
            ResolutionMode = AzureCredentialResolutionMode.ManagedIdentity,
            ManagedIdentityClientIdSettingName = "AzureRuntime:Identity:ManagedIdentityClientId",
            TenantIdSettingName = "AzureRuntime:Identity:TenantId",
            AllowsDeveloperCredential = true,
            AllowsConnectionStringFallback = false
        });

        Add(new AzureCredentialBoundary
        {
            Name = "artifact-storage",
            Description = "Credential boundary for migration artifacts, manifests, exports, and evidence.",
            ResolutionMode = AzureCredentialResolutionMode.ManagedIdentity,
            ManagedIdentityClientIdSettingName = "AzureRuntime:Identity:ManagedIdentityClientId",
            TenantIdSettingName = "AzureRuntime:Identity:TenantId",
            AllowsDeveloperCredential = true,
            AllowsConnectionStringFallback = false
        });

        Add(new AzureCredentialBoundary
        {
            Name = "queue-runtime",
            Description = "Credential boundary for queue, dispatcher, and worker execution surfaces.",
            ResolutionMode = AzureCredentialResolutionMode.ManagedIdentity,
            ManagedIdentityClientIdSettingName = "AzureRuntime:Identity:ManagedIdentityClientId",
            TenantIdSettingName = "AzureRuntime:Identity:TenantId",
            AllowsDeveloperCredential = true,
            AllowsConnectionStringFallback = false
        });
    }

    public IReadOnlyCollection<AzureCredentialBoundary> GetAll() => _boundaries;

    public AzureCredentialBoundary? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _boundaries.FirstOrDefault(boundary => string.Equals(boundary.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void Add(AzureCredentialBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        if (string.IsNullOrWhiteSpace(boundary.Name))
        {
            throw new ArgumentException("Credential boundary name is required.", nameof(boundary));
        }

        if (FindByName(boundary.Name) is not null)
        {
            return;
        }

        _boundaries.Add(boundary);
    }
}
