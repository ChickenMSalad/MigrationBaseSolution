namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Runtime correlation context used by workers, APIs, operators, and deployment checks.
/// </summary>
public sealed class AzureCorrelationContext
{
    public AzureCorrelationContext(AzureCorrelationScopeDescriptor descriptor)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    public AzureCorrelationScopeDescriptor Descriptor { get; }

    public bool HasRunScope => !string.IsNullOrWhiteSpace(Descriptor.RunId);

    public bool HasWorkItemScope => !string.IsNullOrWhiteSpace(Descriptor.WorkItemId);

    public IReadOnlyDictionary<string, string> ToLogDimensions()
    {
        var dimensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["correlation.id"] = Descriptor.CorrelationId
        };

        AddIfPresent(dimensions, "correlation.parent_id", Descriptor.ParentCorrelationId);
        AddIfPresent(dimensions, "migration.run_id", Descriptor.RunId);
        AddIfPresent(dimensions, "migration.work_item_id", Descriptor.WorkItemId);
        AddIfPresent(dimensions, "worker.instance_id", Descriptor.WorkerInstanceId);
        AddIfPresent(dimensions, "host.role", Descriptor.HostRole);
        AddIfPresent(dimensions, "environment.name", Descriptor.EnvironmentName);
        AddIfPresent(dimensions, "deployment.ring", Descriptor.DeploymentRing);

        foreach (var pair in Descriptor.Dimensions)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            {
                dimensions[pair.Key] = pair.Value;
            }
        }

        return dimensions;
    }

    private static void AddIfPresent(IDictionary<string, string> dimensions, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            dimensions[key] = value;
        }
    }
}
