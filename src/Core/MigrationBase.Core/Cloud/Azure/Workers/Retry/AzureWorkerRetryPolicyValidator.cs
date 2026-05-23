namespace MigrationBase.Core.Cloud.Azure.Workers.Retry;

public sealed class AzureWorkerRetryPolicyValidator
{
    public AzureWorkerRetryPolicyValidationResult Validate(AzureWorkerRetryPolicyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(descriptor.Name))
        {
            errors.Add("Retry policy name is required.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.WorkloadRole))
        {
            errors.Add("Retry policy workload role is required.");
        }

        if (descriptor.MaxAttempts < 1)
        {
            errors.Add("Retry policy MaxAttempts must be at least 1.");
        }

        if (descriptor.InitialDelay < TimeSpan.Zero)
        {
            errors.Add("Retry policy InitialDelay cannot be negative.");
        }

        if (descriptor.MaxDelay < descriptor.InitialDelay)
        {
            errors.Add("Retry policy MaxDelay must be greater than or equal to InitialDelay.");
        }

        if (descriptor.BackoffMultiplier < 1.0d)
        {
            errors.Add("Retry policy BackoffMultiplier must be greater than or equal to 1.0.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.FailureDisposition))
        {
            errors.Add("Retry policy FailureDisposition is required.");
        }

        return errors.Count == 0
            ? AzureWorkerRetryPolicyValidationResult.Valid()
            : AzureWorkerRetryPolicyValidationResult.Invalid(errors);
    }
}
