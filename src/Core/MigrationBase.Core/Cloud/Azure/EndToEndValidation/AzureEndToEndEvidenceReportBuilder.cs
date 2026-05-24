using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndEvidenceReportBuilder :
    IAzureEndToEndEvidenceReportBuilder
{
    public AzureEndToEndEvidenceReport Build(
        AzureEndToEndEvidenceReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ValidationResult);

        var evidence = new List<AzureEndToEndEvidenceEntry>
        {
            CreateEvidence("validation.status", request.ValidationResult.Status.ToString(), "scenario-validation"),
            CreateEvidence("validation.passed", request.ValidationResult.Passed.ToString(), "scenario-validation"),
            CreateEvidence("validation.issueCount", request.ValidationResult.Issues.Count.ToString(), "scenario-validation")
        };

        var issues = new List<AzureEndToEndValidationIssue>(request.ValidationResult.Issues);

        if (request.DryRunResult is null)
        {
            if (request.RequireDryRunResult)
            {
                issues.Add(
                    new AzureEndToEndValidationIssue
                    {
                        Code = "e2e.evidence.dryrun.missing",
                        Component = "dry-run",
                        Message = "Dry-run result is required but was not provided.",
                        IsWarning = false
                    });
            }
        }
        else
        {
            evidence.Add(CreateEvidence("dryRun.passed", request.DryRunResult.Passed.ToString(), "dry-run"));
            evidence.Add(CreateEvidence("dryRun.stepCount", request.DryRunResult.Steps.Count.ToString(), "dry-run"));

            foreach (var step in request.DryRunResult.Steps)
            {
                evidence.Add(
                    CreateEvidence(
                        $"dryRun.step.{step.StepId}.status",
                        step.Status.ToString(),
                        "dry-run"));
            }
        }

        return new AzureEndToEndEvidenceReport
        {
            ReportId = Guid.NewGuid().ToString("n"),
            ScenarioId = request.ValidationResult.ScenarioId,
            Status = DetermineStatus(request.ValidationResult, request.DryRunResult, issues),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Evidence = evidence,
            Issues = issues
        };
    }

    private static AzureEndToEndEvidenceReportStatus DetermineStatus(
        AzureEndToEndValidationResult validationResult,
        AzureEndToEndDryRunResult? dryRunResult,
        IReadOnlyCollection<AzureEndToEndValidationIssue> issues)
    {
        if (issues.Any(issue => !issue.IsWarning))
        {
            return AzureEndToEndEvidenceReportStatus.Failed;
        }

        if (!validationResult.Passed || (dryRunResult is not null && !dryRunResult.Passed))
        {
            return AzureEndToEndEvidenceReportStatus.Failed;
        }

        if (issues.Any(issue => issue.IsWarning) || validationResult.HasWarnings)
        {
            return AzureEndToEndEvidenceReportStatus.PassedWithWarnings;
        }

        return AzureEndToEndEvidenceReportStatus.Passed;
    }

    private static AzureEndToEndEvidenceEntry CreateEvidence(
        string key,
        string value,
        string source)
    {
        return new AzureEndToEndEvidenceEntry
        {
            Key = key,
            Value = value,
            Source = source,
            RecordedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
