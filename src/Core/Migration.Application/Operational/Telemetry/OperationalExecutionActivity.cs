using System.Diagnostics;

namespace Migration.Application.Operational.Telemetry;

public static class OperationalExecutionActivity
{
    public static Activity? StartSqlQueueWorkItemExecution(
        Guid runId,
        Guid workItemId,
        Guid? manifestRowId = null,
        string? workItemType = null,
        int? attemptCount = null,
        string? partitionKey = null)
    {
        var activity = OperationalExecutionActivitySources.Source.StartActivity(
            OperationalExecutionActivitySources.SqlQueueWorkItemExecution,
            ActivityKind.Internal);

        AddCommonTags(activity, runId, workItemId, manifestRowId, workItemType, attemptCount, partitionKey);
        return activity;
    }

    public static Activity? StartServiceBusWorkItemExecution(
        Guid runId,
        Guid workItemId,
        Guid? manifestRowId = null,
        string? workItemType = null,
        int? attemptCount = null,
        string? partitionKey = null,
        string? serviceBusCorrelationId = null,
        string? serviceBusMessageId = null,
        long? serviceBusDeliveryCount = null)
    {
        var activity = OperationalExecutionActivitySources.Source.StartActivity(
            OperationalExecutionActivitySources.ServiceBusWorkItemExecution,
            ActivityKind.Consumer);

        AddCommonTags(activity, runId, workItemId, manifestRowId, workItemType, attemptCount, partitionKey);
        AddIfPresent(activity, OperationalExecutionActivityTags.ServiceBusCorrelationId, serviceBusCorrelationId);
        AddIfPresent(activity, OperationalExecutionActivityTags.ServiceBusMessageId, serviceBusMessageId);
        if (serviceBusDeliveryCount.HasValue)
        {
            activity?.SetTag(OperationalExecutionActivityTags.ServiceBusDeliveryCount, serviceBusDeliveryCount.Value);
        }

        return activity;
    }

    public static Activity? StartServiceBusDispatch(
        Guid runId,
        Guid workItemId,
        Guid? manifestRowId = null,
        string? workItemType = null,
        string? serviceBusCorrelationId = null,
        string? serviceBusMessageId = null)
    {
        var activity = OperationalExecutionActivitySources.Source.StartActivity(
            OperationalExecutionActivitySources.ServiceBusDispatch,
            ActivityKind.Producer);

        AddCommonTags(activity, runId, workItemId, manifestRowId, workItemType, null, null);
        AddIfPresent(activity, OperationalExecutionActivityTags.ServiceBusCorrelationId, serviceBusCorrelationId);
        AddIfPresent(activity, OperationalExecutionActivityTags.ServiceBusMessageId, serviceBusMessageId);
        return activity;
    }

    public static void SetExecutionDuration(Activity? activity, TimeSpan duration)
    {
        activity?.SetTag(OperationalExecutionActivityTags.ExecutionDurationMs, duration.TotalMilliseconds);
    }

    public static void SetExecutionResult(Activity? activity, bool succeeded, string? errorCode = null)
    {
        activity?.SetTag(OperationalExecutionActivityTags.ExecutionSucceeded, succeeded);
        AddIfPresent(activity, OperationalExecutionActivityTags.ErrorCode, errorCode);
        if (!succeeded)
        {
            activity?.SetStatus(ActivityStatusCode.Error, errorCode);
        }
    }

    private static void AddCommonTags(
        Activity? activity,
        Guid runId,
        Guid workItemId,
        Guid? manifestRowId,
        string? workItemType,
        int? attemptCount,
        string? partitionKey)
    {
        activity?.SetTag(OperationalExecutionActivityTags.RunId, runId.ToString("D"));
        activity?.SetTag(OperationalExecutionActivityTags.WorkItemId, workItemId.ToString("D"));
        if (manifestRowId.HasValue)
        {
            activity?.SetTag(OperationalExecutionActivityTags.ManifestRowId, manifestRowId.Value.ToString("D"));
        }

        AddIfPresent(activity, OperationalExecutionActivityTags.WorkItemType, workItemType);
        if (attemptCount.HasValue)
        {
            activity?.SetTag(OperationalExecutionActivityTags.AttemptCount, attemptCount.Value);
        }

        AddIfPresent(activity, OperationalExecutionActivityTags.PartitionKey, partitionKey);
    }

    private static void AddIfPresent(Activity? activity, string tagName, string? value)
    {
        if (activity is not null && !string.IsNullOrWhiteSpace(value))
        {
            activity.SetTag(tagName, value);
        }
    }
}
