namespace MigrationBase.Core.Cloud.Azure.ExecutionValidation;

public static class AzureReplayValidationRules
{
    public static AzureReplayValidationResult Validate(AzureReplayValidationDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(descriptor.ValidationId))
        {
            errors.Add("ValidationId is required.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.MigrationRunId))
        {
            errors.Add("MigrationRunId is required.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.SourceSystem))
        {
            errors.Add("SourceSystem is required.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.TargetSystem))
        {
            errors.Add("TargetSystem is required.");
        }

        if (descriptor.Mode is AzureReplayValidationMode.ApprovedReplay or AzureReplayValidationMode.EmergencyReplay)
        {
            if (!descriptor.RequireSourceTargetMappingVerification)
            {
                errors.Add("Approved or emergency replay requires source/target mapping verification.");
            }

            if (!descriptor.RequireIdempotencyEvidence)
            {
                errors.Add("Approved or emergency replay requires idempotency evidence.");
            }
        }

        return errors.Count == 0
            ? AzureReplayValidationResult.Success(descriptor.ValidationId)
            : AzureReplayValidationResult.Failure(descriptor.ValidationId, errors);
    }
}
