namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Performs basic contract validation for structured operational log events before emission.
/// </summary>
public static class AzureStructuredLogEventValidator
{
    public static IReadOnlyList<string> Validate(AzureStructuredLogEvent logEvent, AzureStructuredLogEventDescriptor? descriptor = null)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(logEvent.EventName))
        {
            errors.Add("EventName is required.");
        }

        if (string.IsNullOrWhiteSpace(logEvent.EventCategory))
        {
            errors.Add("EventCategory is required.");
        }

        if (descriptor is not null)
        {
            if (descriptor.RequiresCorrelationId && string.IsNullOrWhiteSpace(logEvent.CorrelationId))
            {
                errors.Add($"CorrelationId is required for event '{descriptor.EventName}'.");
            }

            if (descriptor.RequiresMigrationRunId && string.IsNullOrWhiteSpace(logEvent.MigrationRunId))
            {
                errors.Add($"MigrationRunId is required for event '{descriptor.EventName}'.");
            }

            if (descriptor.RequiresWorkItemId && string.IsNullOrWhiteSpace(logEvent.WorkItemId))
            {
                errors.Add($"WorkItemId is required for event '{descriptor.EventName}'.");
            }
        }

        return errors;
    }
}
