using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionValidator : IAzureConnectorExecutionValidator
{
    public AzureConnectorExecutionValidationResult Validate(
        AzureConnectorExecutionRequest request,
        AzureConnectorExecutionValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveOptions = options ?? new AzureConnectorExecutionValidationOptions();
        var issues = new List<AzureConnectorExecutionValidationIssue>();

        AddRequiredIssueIfMissing(
            issues,
            effectiveOptions.RequireRunId,
            request.RunId,
            "runId",
            "connector.request.runId.required",
            "RunId is required for connector execution.");

        AddRequiredIssueIfMissing(
            issues,
            effectiveOptions.RequireManifestId,
            request.ManifestId,
            "manifestId",
            "connector.request.manifestId.required",
            "ManifestId is required for connector execution.");

        AddRequiredIssueIfMissing(
            issues,
            effectiveOptions.RequireItemId,
            request.ItemId,
            "itemId",
            "connector.request.itemId.required",
            "ItemId is required for connector execution.");

        AddRequiredIssueIfMissing(
            issues,
            effectiveOptions.RequireSourceIdentifier,
            request.SourceIdentifier,
            "sourceIdentifier",
            "connector.request.sourceIdentifier.required",
            "SourceIdentifier is required for connector execution.");

        AddRequiredIssueIfMissing(
            issues,
            effectiveOptions.RequireTargetIdentifierForWrite &&
                request.Direction == AzureConnectorExecutionDirection.TargetWrite,
            request.TargetIdentifier,
            "targetIdentifier",
            "connector.request.targetIdentifier.required",
            "TargetIdentifier is required for target write connector execution.");

        return issues.Count == 0
            ? AzureConnectorExecutionValidationResult.Success
            : new AzureConnectorExecutionValidationResult { Issues = issues };
    }

    private static void AddRequiredIssueIfMissing(
        ICollection<AzureConnectorExecutionValidationIssue> issues,
        bool required,
        string? value,
        string field,
        string code,
        string message)
    {
        if (!required || !string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        issues.Add(
            new AzureConnectorExecutionValidationIssue
            {
                Code = code,
                Field = field,
                Message = message,
                Severity = AzureConnectorExecutionValidationSeverity.Error
            });
    }
}
