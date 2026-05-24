using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationRunner : IAzureEndToEndValidationRunner
{
    public Task<AzureEndToEndValidationResult> RunAsync(
        AzureEndToEndValidationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Scenario);

        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<AzureEndToEndValidationIssue>();

        AddRequiredCapabilityIssue(
            issues,
            request.Scenario.RequiresQueueDispatch,
            "queue-dispatch",
            "End-to-end validation scenario requires queue dispatch.");

        AddRequiredCapabilityIssue(
            issues,
            request.Scenario.RequiresManifestExecution,
            "manifest-execution",
            "End-to-end validation scenario requires manifest execution.");

        AddRequiredCapabilityIssue(
            issues,
            request.Scenario.RequiresConnectorExecution,
            "connector-execution",
            "End-to-end validation scenario requires connector execution.");

        AddRequiredCapabilityIssue(
            issues,
            request.Scenario.RequiresFailureRuntime,
            "failure-runtime",
            "End-to-end validation scenario requires failure runtime.");

        var status = DetermineStatus(issues, request.TreatWarningsAsFailures);

        return Task.FromResult(
            new AzureEndToEndValidationResult
            {
                ScenarioId = request.Scenario.ScenarioId,
                Status = status,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                Issues = issues
            });
    }

    private static void AddRequiredCapabilityIssue(
        ICollection<AzureEndToEndValidationIssue> issues,
        bool required,
        string component,
        string message)
    {
        if (!required)
        {
            return;
        }

        issues.Add(
            new AzureEndToEndValidationIssue
            {
                Code = "e2e.validation.capability.required",
                Component = component,
                Message = message,
                IsWarning = true
            });
    }

    private static AzureEndToEndValidationStatus DetermineStatus(
        IReadOnlyCollection<AzureEndToEndValidationIssue> issues,
        bool treatWarningsAsFailures)
    {
        if (issues.Count == 0)
        {
            return AzureEndToEndValidationStatus.Passed;
        }

        var hasErrors = false;
        var hasWarnings = false;

        foreach (var issue in issues)
        {
            if (issue.IsWarning)
            {
                hasWarnings = true;
            }
            else
            {
                hasErrors = true;
            }
        }

        if (hasErrors || (hasWarnings && treatWarningsAsFailures))
        {
            return AzureEndToEndValidationStatus.Failed;
        }

        return AzureEndToEndValidationStatus.PassedWithWarnings;
    }
}
