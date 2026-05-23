namespace MigrationBase.Core.Cloud.Azure.Infrastructure;

public sealed class AzureInfrastructureClientRegistry : IAzureInfrastructureClientRegistry
{
    private readonly IReadOnlyCollection<AzureInfrastructureClientDescriptor> _clients;

    public AzureInfrastructureClientRegistry(IEnumerable<AzureInfrastructureClientDescriptor> clients)
    {
        _clients = clients?.ToArray() ?? Array.Empty<AzureInfrastructureClientDescriptor>();
    }

    public IReadOnlyCollection<AzureInfrastructureClientDescriptor> GetClients()
    {
        return _clients;
    }

    public AzureInfrastructureClientDescriptor? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _clients.FirstOrDefault(client => string.Equals(client.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public AzureInfrastructureClientValidationResult Validate()
    {
        var result = new AzureInfrastructureClientValidationResult();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var client in _clients)
        {
            if (string.IsNullOrWhiteSpace(client.Name))
            {
                result.AddIssue("azure.infrastructure.client.name.missing", "Infrastructure client name is required.");
                continue;
            }

            if (!seen.Add(client.Name))
            {
                result.AddIssue("azure.infrastructure.client.name.duplicate", "Infrastructure client names must be unique.", client.Name);
            }

            if (client.Kind == AzureInfrastructureClientKind.Unknown)
            {
                result.AddIssue("azure.infrastructure.client.kind.unknown", "Infrastructure client kind must be specified.", client.Name);
            }

            foreach (var setting in client.RequiredSettings)
            {
                if (string.IsNullOrWhiteSpace(setting))
                {
                    result.AddIssue("azure.infrastructure.client.requiredSetting.empty", "Required setting names cannot be empty.", client.Name);
                }
            }
        }

        return result;
    }
}
