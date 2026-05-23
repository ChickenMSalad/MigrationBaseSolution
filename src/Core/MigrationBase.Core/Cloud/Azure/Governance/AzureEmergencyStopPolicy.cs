namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed class AzureEmergencyStopPolicy : IAzureEmergencyStopPolicy
{
    public AzureEmergencyStopValidationResult Validate(AzureEmergencyStopDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var result = AzureEmergencyStopValidationResult.Success();

        if (string.IsNullOrWhiteSpace(descriptor.Name))
        {
            result.Errors.Add("Emergency stop name is required.");
        }

        if (descriptor.Mode == AzureEmergencyStopMode.None)
        {
            result.Errors.Add("Emergency stop mode must be specified.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.Reason))
        {
            result.Errors.Add("Emergency stop reason is required.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.RequestedBy))
        {
            result.Errors.Add("Emergency stop requester is required.");
        }

        if (descriptor.Mode == AzureEmergencyStopMode.HardStop && descriptor.AllowsActiveWorkToDrain)
        {
            result.Warnings.Add("Hard stop normally should not allow active work to drain.");
        }

        return result;
    }

    public AzureEmergencyStopValidationResult ValidateDrain(AzureDrainRequestDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var result = AzureEmergencyStopValidationResult.Success();

        if (string.IsNullOrWhiteSpace(descriptor.Name))
        {
            result.Errors.Add("Drain request name is required.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.EnvironmentName))
        {
            result.Errors.Add("Drain request environment name is required.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.TargetRole))
        {
            result.Errors.Add("Drain request target role is required.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.RequestedBy))
        {
            result.Errors.Add("Drain request requester is required.");
        }

        return result;
    }
}
