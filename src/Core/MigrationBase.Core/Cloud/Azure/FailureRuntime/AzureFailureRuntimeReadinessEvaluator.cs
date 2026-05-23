using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureRuntimeReadinessEvaluator :
    IAzureFailureRuntimeReadinessEvaluator
{
    private readonly IAzureFailureClassifier? failureClassifier;
    private readonly IAzureRetryDecisionEngine? retryDecisionEngine;
    private readonly IAzureReplayEligibilityEvaluator? replayEligibilityEvaluator;
    private readonly IAzureReplayAdmissionController? replayAdmissionController;
    private readonly IAzureFailureIncidentStore? incidentStore;

    public AzureFailureRuntimeReadinessEvaluator(
        IAzureFailureClassifier? failureClassifier = null,
        IAzureRetryDecisionEngine? retryDecisionEngine = null,
        IAzureReplayEligibilityEvaluator? replayEligibilityEvaluator = null,
        IAzureReplayAdmissionController? replayAdmissionController = null,
        IAzureFailureIncidentStore? incidentStore = null)
    {
        this.failureClassifier = failureClassifier;
        this.retryDecisionEngine = retryDecisionEngine;
        this.replayEligibilityEvaluator = replayEligibilityEvaluator;
        this.replayAdmissionController = replayAdmissionController;
        this.incidentStore = incidentStore;
    }

    public Task<AzureFailureRuntimeReadinessReport> EvaluateAsync(
        AzureFailureRuntimeReadinessRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<AzureFailureRuntimeReadinessIssue>();

        AddMissingIssueIfRequired(
            issues,
            request.RequireFailureClassifier,
            failureClassifier,
            "failure-classifier",
            "Failure classifier is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireRetryDecisionEngine,
            retryDecisionEngine,
            "retry-decision-engine",
            "Retry decision engine is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireReplayEligibilityEvaluator,
            replayEligibilityEvaluator,
            "replay-eligibility-evaluator",
            "Replay eligibility evaluator is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireReplayAdmissionController,
            replayAdmissionController,
            "replay-admission-controller",
            "Replay admission controller is not registered.");

        AddMissingIssueIfRequired(
            issues,
            request.RequireIncidentStore,
            incidentStore,
            "failure-incident-store",
            "Failure incident store is not registered.");

        var status = issues.Count == 0
            ? AzureFailureRuntimeReadinessStatus.Ready
            : AzureFailureRuntimeReadinessStatus.NotReady;

        return Task.FromResult(
            new AzureFailureRuntimeReadinessReport
            {
                Status = status,
                EvaluatedAtUtc = DateTimeOffset.UtcNow,
                Issues = issues
            });
    }

    private static void AddMissingIssueIfRequired(
        ICollection<AzureFailureRuntimeReadinessIssue> issues,
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
            new AzureFailureRuntimeReadinessIssue
            {
                Code = "failure.runtime.component.missing",
                Component = component,
                Message = message
            });
    }
}
