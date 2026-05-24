using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MigrationBase.Core.Cloud.Azure.ManifestExecution;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionReadinessEvaluator :
    IAzureConnectorExecutionReadinessEvaluator
{
    private readonly IAzureConnectorExecutor? connectorExecutor;
    private readonly IAzureManifestExecutionItemHandler? manifestItemHandler;
    private readonly IAzureConnectorExecutionValidator? executionValidator;
    private readonly IAzureConnectorExecutionPreflight? preflight;
    private readonly IAzureConnectorExecutionEvidenceSink? evidenceSink;

    public AzureConnectorExecutionReadinessEvaluator(
        IAzureConnectorExecutor? connectorExecutor = null,
        IAzureManifestExecutionItemHandler? manifestItemHandler = null,
        IAzureConnectorExecutionValidator? executionValidator = null,
        IAzureConnectorExecutionPreflight? preflight = null,
        IAzureConnectorExecutionEvidenceSink? evidenceSink = null)
    {
        this.connectorExecutor = connectorExecutor;
        this.manifestItemHandler = manifestItemHandler;
        this.executionValidator = executionValidator;
        this.preflight = preflight;
        this.evidenceSink = evidenceSink;
    }

    public Task<AzureConnectorExecutionReadinessReport> EvaluateAsync(
        AzureConnectorExecutionReadinessRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<AzureConnectorExecutionReadinessIssue>();

        AddMissingIssueIfRequired(
            issues,
            request.RequireConnectorExecutor,
            connectorExecutor,
            "connector-executor",
            "Connector executor is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireManifestItemHandler,
            manifestItemHandler,
            "manifest-item-handler",
            "Manifest item handler adapter is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireExecutionValidator,
            executionValidator,
            "execution-validator",
            "Connector execution validator is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequirePreflight,
            preflight,
            "preflight",
            "Connector execution preflight is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireEvidenceSink,
            evidenceSink,
            "evidence-sink",
            "Connector execution evidence sink is not registered.");

        var status = issues.Count == 0
            ? AzureConnectorExecutionReadinessStatus.Ready
            : AzureConnectorExecutionReadinessStatus.NotReady;

        return Task.FromResult(
            new AzureConnectorExecutionReadinessReport
            {
                Status = status,
                EvaluatedAtUtc = DateTimeOffset.UtcNow,
                Issues = issues
            });
    }

    private static void AddMissingIssueIfRequired(
        ICollection<AzureConnectorExecutionReadinessIssue> issues,
        bool required,
        object? service,
        string component,
        string message)
    {
        if (!required || service is not null)
        {
            return;
        }

        issues.Add(
            new AzureConnectorExecutionReadinessIssue
            {
                Code = "connector.execution.component.missing",
                Component = component,
                Message = message
            });
    }
}
