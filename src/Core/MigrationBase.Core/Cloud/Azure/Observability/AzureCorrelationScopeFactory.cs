namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Creates stable correlation descriptors without binding the core layer to any telemetry provider.
/// </summary>
public sealed class AzureCorrelationScopeFactory
{
    public AzureCorrelationContext Create(AzureCorrelationScopeDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.CorrelationId))
        {
            descriptor = descriptor.WithGeneratedCorrelationId();
        }

        return new AzureCorrelationContext(descriptor);
    }
}

internal static class AzureCorrelationScopeDescriptorExtensions
{
    public static AzureCorrelationScopeDescriptor WithGeneratedCorrelationId(this AzureCorrelationScopeDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return new AzureCorrelationScopeDescriptor
        {
            CorrelationId = Guid.NewGuid().ToString("N"),
            ParentCorrelationId = descriptor.ParentCorrelationId,
            RunId = descriptor.RunId,
            WorkItemId = descriptor.WorkItemId,
            WorkerInstanceId = descriptor.WorkerInstanceId,
            HostRole = descriptor.HostRole,
            EnvironmentName = descriptor.EnvironmentName,
            DeploymentRing = descriptor.DeploymentRing,
            StartedAtUtc = descriptor.StartedAtUtc,
            Dimensions = descriptor.Dimensions
        };
    }
}
