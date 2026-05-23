using System;

namespace MigrationBase.Core.Cloud.Azure.FailureRuntime;

public sealed class AzureFailureClassifier : IAzureFailureClassifier
{
    public AzureFailureClassificationResult Classify(AzureFailureSignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);

        var errorCode = signal.ErrorCode ?? string.Empty;
        var message = signal.Message ?? string.Empty;

        if (Contains(errorCode, "poison") || Contains(message, "poison"))
        {
            return new AzureFailureClassificationResult
            {
                Classification = AzureFailureClassification.Poison,
                Severity = AzureFailureSeverity.Critical,
                RetryRecommended = false,
                ReplayRecommended = false,
                Reason = "Failure signal indicates poison work."
            };
        }

        if (Contains(errorCode, "timeout") ||
            Contains(errorCode, "throttle") ||
            Contains(message, "timeout") ||
            Contains(message, "throttle"))
        {
            return new AzureFailureClassificationResult
            {
                Classification = AzureFailureClassification.Transient,
                Severity = AzureFailureSeverity.Warning,
                RetryRecommended = true,
                ReplayRecommended = false,
                Reason = "Failure signal appears transient."
            };
        }

        if (Contains(errorCode, "validation") ||
            Contains(errorCode, "notfound") ||
            Contains(message, "validation"))
        {
            return new AzureFailureClassificationResult
            {
                Classification = AzureFailureClassification.Permanent,
                Severity = AzureFailureSeverity.Error,
                RetryRecommended = false,
                ReplayRecommended = false,
                Reason = "Failure signal appears permanent without operator correction."
            };
        }

        if (signal.AttemptNumber > 1)
        {
            return new AzureFailureClassificationResult
            {
                Classification = AzureFailureClassification.ReplayEligible,
                Severity = AzureFailureSeverity.Error,
                RetryRecommended = false,
                ReplayRecommended = true,
                Reason = "Failure occurred after prior attempts and may need replay evaluation."
            };
        }

        return new AzureFailureClassificationResult
        {
            Classification = AzureFailureClassification.Unknown,
            Severity = AzureFailureSeverity.Error,
            RetryRecommended = false,
            ReplayRecommended = false,
            Reason = "No classification rule matched the failure signal."
        };
    }

    private static bool Contains(string value, string expected)
    {
        return value.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }
}
