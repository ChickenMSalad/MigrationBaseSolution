namespace MigrationBase.Core.Cloud.Azure.Governance;

public interface IAzureEmergencyStopPolicy
{
    AzureEmergencyStopValidationResult Validate(AzureEmergencyStopDescriptor descriptor);
    AzureEmergencyStopValidationResult ValidateDrain(AzureDrainRequestDescriptor descriptor);
}
