using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationReadinessEvaluator :
    IAzureEndToEndValidationReadinessEvaluator
{
    private readonly IAzureEndToEndValidationRunner? validationRunner;
    private readonly IAzureEndToEndDryRunHarness? dryRunHarness;
    private readonly IAzureEndToEndEvidenceReportBuilder? evidenceReportBuilder;

    public AzureEndToEndValidationReadinessEvaluator(
        IAzureEndToEndValidationRunner? validationRunner = null,
        IAzureEndToEndDryRunHarness? dryRunHarness = null,
        IAzureEndToEndEvidenceReportBuilder? evidenceReportBuilder = null)
    {
        this.validationRunner = validationRunner;
        this.dryRunHarness = dryRunHarness;
        this.evidenceReportBuilder = evidenceReportBuilder;
    }

    public Task<AzureEndToEndValidationReadinessReport> EvaluateAsync(
        AzureEndToEndValidationReadinessRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<AzureEndToEndValidationReadinessIssue>();

        AddMissingIssueIfRequired(
            issues,
            request.RequireValidationRunner,
            validationRunner,
            "validation-runner",
            "End-to-end validation runner is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireDryRunHarness,
            dryRunHarness,
            "dry-run-harness",
            "End-to-end dry-run harness is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireEvidenceReportBuilder,
            evidenceReportBuilder,
            "evidence-report-builder",
            "End-to-end evidence report builder is not registered.");

        var status = issues.Count == 0
            ? AzureEndToEndValidationReadinessStatus.Ready
            : AzureEndToEndValidationReadinessStatus.NotReady;

        return Task.FromResult(
            new AzureEndToEndValidationReadinessReport
            {
                Status = status,
                EvaluatedAtUtc = DateTimeOffset.UtcNow,
                Issues = issues
            });
    }

    private static void AddMissingIssueIfRequired(
        ICollection<AzureEndToEndValidationReadinessIssue> issues,
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
            new AzureEndToEndValidationReadinessIssue
            {
                Code = "e2e.validation.component.missing",
                Component = component,
                Message = message
            });
    }
}
