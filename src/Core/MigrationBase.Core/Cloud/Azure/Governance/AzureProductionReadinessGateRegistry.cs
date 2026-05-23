namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed class AzureProductionReadinessGateRegistry : IAzureProductionReadinessGateRegistry
{
    private readonly IReadOnlyDictionary<string, AzureProductionReadinessGate> gatesById;

    public AzureProductionReadinessGateRegistry(IEnumerable<AzureProductionReadinessGate> gates)
    {
        if (gates is null)
        {
            throw new ArgumentNullException(nameof(gates));
        }

        this.gatesById = gates
            .Where(gate => !string.IsNullOrWhiteSpace(gate.GateId))
            .GroupBy(gate => gate.GateId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<AzureProductionReadinessGate> GetGates()
    {
        return gatesById.Values.ToArray();
    }

    public AzureProductionReadinessGate? TryGetGate(string gateId)
    {
        if (string.IsNullOrWhiteSpace(gateId))
        {
            return null;
        }

        return gatesById.TryGetValue(gateId, out AzureProductionReadinessGate? gate) ? gate : null;
    }
}
