namespace Migration.Application.Operational.Telemetry;

public static class OperationalExecutionTelemetryScope
{
    public static IReadOnlyDictionary<string, object?> Create(
        Guid runId,
        long workItemId,
        string? workerId = null,
        string? serviceBusMessageId = null,
        string? serviceBusCorrelationId = null,
        int? serviceBusDeliveryCount = null)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [OperationalExecutionTelemetryFields.RunId] = runId,
            [OperationalExecutionTelemetryFields.WorkItemId] = workItemId
        };

        AddIfPresent(values, "WorkerId", workerId);
        AddIfPresent(values, "ServiceBusMessageId", serviceBusMessageId);
        AddIfPresent(values, OperationalExecutionTelemetryFields.ServiceBusCorrelationId, serviceBusCorrelationId);

        if (serviceBusDeliveryCount.HasValue)
        {
            values["ServiceBusDeliveryCount"] = serviceBusDeliveryCount.Value;
        }

        return values;
    }

    private static void AddIfPresent(IDictionary<string, object?> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values[key] = value;
        }
    }
}
