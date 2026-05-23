namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// In-memory registry for known Azure operational structured log events.
/// </summary>
public sealed class AzureStructuredLogEventRegistry : IAzureStructuredLogEventRegistry
{
    private readonly IReadOnlyDictionary<string, AzureStructuredLogEventDescriptor> descriptors;

    public AzureStructuredLogEventRegistry(IEnumerable<AzureStructuredLogEventDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        this.descriptors = descriptors
            .Where(descriptor => !string.IsNullOrWhiteSpace(descriptor.EventName))
            .GroupBy(descriptor => descriptor.EventName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<AzureStructuredLogEventDescriptor> GetDescriptors()
    {
        return descriptors.Values.ToArray();
    }

    public AzureStructuredLogEventDescriptor? FindByName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            return null;
        }

        return descriptors.TryGetValue(eventName, out var descriptor) ? descriptor : null;
    }
}
