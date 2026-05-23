namespace MigrationBase.Core.Cloud.Azure.Hosting;

/// <summary>
/// In-memory host role registry used by deployment, diagnostics, and worker stabilization code.
/// </summary>
public sealed class AzureHostRoleRegistry
{
    private readonly List<AzureHostRoleDescriptor> descriptors = new();

    public IReadOnlyList<AzureHostRoleDescriptor> Descriptors => descriptors;

    public AzureHostRoleRegistry Add(AzureHostRoleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (string.IsNullOrWhiteSpace(descriptor.HostName))
        {
            throw new ArgumentException("Host role descriptors must include a host name.", nameof(descriptor));
        }

        if (descriptor.RoleKind == AzureHostRoleKind.Unknown)
        {
            throw new ArgumentException("Host role descriptors must include a concrete role kind.", nameof(descriptor));
        }

        descriptors.Add(descriptor);
        return this;
    }

    public AzureHostRoleDescriptor? FindByHostName(string hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return null;
        }

        return descriptors.FirstOrDefault(x => string.Equals(x.HostName, hostName, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<AzureHostRoleDescriptor> FindByRoleKind(AzureHostRoleKind roleKind)
    {
        return descriptors.Where(x => x.RoleKind == roleKind).ToArray();
    }
}
