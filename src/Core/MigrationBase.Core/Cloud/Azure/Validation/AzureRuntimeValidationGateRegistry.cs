namespace MigrationBase.Core.Cloud.Azure.Validation;

public sealed class AzureRuntimeValidationGateRegistry
{
    private readonly IReadOnlyList<AzureRuntimeValidationGateDescriptor> _gates;

    public AzureRuntimeValidationGateRegistry(IEnumerable<AzureRuntimeValidationGateDescriptor> gates)
    {
        ArgumentNullException.ThrowIfNull(gates);
        _gates = gates
            .Where(gate => !string.IsNullOrWhiteSpace(gate.GateKey))
            .GroupBy(gate => gate.GateKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(gate => gate.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(gate => gate.GateKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<AzureRuntimeValidationGateDescriptor> Gates => _gates;

    public AzureRuntimeValidationGateDescriptor? Find(string gateKey)
    {
        if (string.IsNullOrWhiteSpace(gateKey))
        {
            return null;
        }

        return _gates.FirstOrDefault(gate => string.Equals(gate.GateKey, gateKey, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<AzureRuntimeValidationGateDescriptor> ForHostRole(string hostRole)
    {
        if (string.IsNullOrWhiteSpace(hostRole))
        {
            return _gates;
        }

        return _gates
            .Where(gate => string.Equals(gate.AppliesToHostRole, "all", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gate.AppliesToHostRole, hostRole, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<AzureRuntimeValidationGateDescriptor> RequiredForDeployment()
    {
        return _gates.Where(gate => gate.RequiredForDeployment).ToArray();
    }
}
