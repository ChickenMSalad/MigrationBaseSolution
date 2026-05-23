namespace MigrationBase.Core.Cloud.Azure.Integration.Queues;

public sealed class AzureQueueBindingRegistry : IAzureQueueBindingRegistry
{
    private readonly IReadOnlyCollection<AzureQueueBindingDescriptor> _bindings;

    public AzureQueueBindingRegistry(IEnumerable<AzureQueueBindingDescriptor>? bindings = null)
    {
        _bindings = (bindings ?? CreateDefaultBindings()).ToArray();
    }

    public IReadOnlyCollection<AzureQueueBindingDescriptor> GetBindings() => _bindings;

    public AzureQueueBindingDescriptor? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _bindings.FirstOrDefault(binding => string.Equals(binding.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public AzureQueueBindingValidationResult Validate()
    {
        var result = new AzureQueueBindingValidationResult();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var binding in _bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.Name)) result.AddError("Queue binding name is required.");
            if (string.IsNullOrWhiteSpace(binding.QueueName)) result.AddError($"Queue binding '{binding.Name}' must declare QueueName.");
            if (!string.IsNullOrWhiteSpace(binding.Name) && !names.Add(binding.Name)) result.AddError($"Duplicate queue binding name '{binding.Name}'.");
            if (!string.IsNullOrWhiteSpace(binding.QueueName) && !queues.Add(binding.QueueName)) result.AddWarning($"Queue '{binding.QueueName}' is used by multiple bindings.");
        }

        return result;
    }

    private static IEnumerable<AzureQueueBindingDescriptor> CreateDefaultBindings()
    {
        yield return new AzureQueueBindingDescriptor { Name = "dispatcher", QueueName = "migration-dispatch", Purpose = "Dispatcher admission and work scheduling." };
        yield return new AzureQueueBindingDescriptor { Name = "worker", QueueName = "migration-work", Purpose = "Worker execution work item delivery." };
        yield return new AzureQueueBindingDescriptor { Name = "poison", QueueName = "migration-poison", Purpose = "Poison work parking and operator triage." };
        yield return new AzureQueueBindingDescriptor { Name = "events", QueueName = "migration-events", Purpose = "Operational event fan-out." };
    }
}
