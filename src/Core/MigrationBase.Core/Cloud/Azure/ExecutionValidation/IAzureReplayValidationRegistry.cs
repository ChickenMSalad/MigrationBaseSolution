namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public interface IAzureReplayValidationRegistry
{
    IReadOnlyList<AzureReplayValidationDescriptor> GetDescriptors();

    AzureReplayValidationDescriptor? FindByValidationId(string validationId);
}
