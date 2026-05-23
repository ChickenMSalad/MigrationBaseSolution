namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public sealed class AzureReplayValidationRegistry : IAzureReplayValidationRegistry
{
    private readonly IReadOnlyList<AzureReplayValidationDescriptor> descriptors;

    public AzureReplayValidationRegistry(IEnumerable<AzureReplayValidationDescriptor> descriptors)
    {
        this.descriptors = descriptors?.ToArray() ?? Array.Empty<AzureReplayValidationDescriptor>();
    }

    public IReadOnlyList<AzureReplayValidationDescriptor> GetDescriptors() => descriptors;

    public AzureReplayValidationDescriptor? FindByValidationId(string validationId)
    {
        if (string.IsNullOrWhiteSpace(validationId))
        {
            return null;
        }

        return descriptors.FirstOrDefault(descriptor =>
            string.Equals(descriptor.ValidationId, validationId, StringComparison.OrdinalIgnoreCase));
    }
}
