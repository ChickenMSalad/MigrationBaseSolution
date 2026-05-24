using System;
using System.Collections.Generic;
using System.Linq;
using MigrationBase.Core.Cloud.Azure.EndToEndValidation;

namespace MigrationBase.Core.Cloud.Azure.ProductionHardening;

public sealed class AzureProductionReleaseGateEvaluator :
    IAzureProductionReleaseGateEvaluator
{
    public AzureProductionReleaseGateResult Evaluate(
        AzureProductionReleaseGateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.EvidenceReport);

        var issues = new List<AzureProductionReleaseGateIssue>();

        if (request.RequirePassedEvidenceReport &&
            request.EvidenceReport.Status == AzureEndToEndEvidenceReportStatus.Failed)
        {
            issues.Add(
                new AzureProductionReleaseGateIssue
                {
                    Code = "production.release.evidence.failed",
                    Component = "end-to-end-evidence",
                    Message = "End-to-end evidence report failed.",
                    IsBlocking = true
                });
        }

        if (request.RequirePassedEvidenceReport &&
            request.EvidenceReport.Status == AzureEndToEndEvidenceReportStatus.Incomplete)
        {
            issues.Add(
                new AzureProductionReleaseGateIssue
                {
                    Code = "production.release.evidence.incomplete",
                    Component = "end-to-end-evidence",
                    Message = "End-to-end evidence report is incomplete.",
                    IsBlocking = true
                });
        }

        if (!request.AllowWarnings &&
            request.EvidenceReport.Status == AzureEndToEndEvidenceReportStatus.PassedWithWarnings)
        {
            issues.Add(
                new AzureProductionReleaseGateIssue
                {
                    Code = "production.release.evidence.warning",
                    Component = "end-to-end-evidence",
                    Message = "End-to-end evidence report contains warnings and warnings are not allowed.",
                    IsBlocking = true
                });
        }

        foreach (var evidenceIssue in request.EvidenceReport.Issues.Where(issue => !issue.IsWarning))
        {
            issues.Add(
                new AzureProductionReleaseGateIssue
                {
                    Code = evidenceIssue.Code,
                    Component = evidenceIssue.Component,
                    Message = evidenceIssue.Message,
                    IsBlocking = true
                });
        }

        var hasBlockingIssues = issues.Any(issue => issue.IsBlocking);

        var status = DetermineStatus(
            request,
            hasBlockingIssues,
            request.EvidenceReport.Status);

        return new AzureProductionReleaseGateResult
        {
            ReleaseId = request.ReleaseId,
            Status = status,
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
            Issues = issues
        };
    }

    private static AzureProductionReleaseGateStatus DetermineStatus(
        AzureProductionReleaseGateRequest request,
        bool hasBlockingIssues,
        AzureEndToEndEvidenceReportStatus evidenceStatus)
    {
        if (hasBlockingIssues && !request.OperatorOverrideGranted)
        {
            return AzureProductionReleaseGateStatus.Blocked;
        }

        if (hasBlockingIssues)
        {
            return AzureProductionReleaseGateStatus.PassedWithWarnings;
        }

        return evidenceStatus switch
        {
            AzureEndToEndEvidenceReportStatus.Passed =>
                AzureProductionReleaseGateStatus.Passed,
            AzureEndToEndEvidenceReportStatus.PassedWithWarnings =>
                AzureProductionReleaseGateStatus.PassedWithWarnings,
            _ =>
                AzureProductionReleaseGateStatus.Failed
        };
    }
}
