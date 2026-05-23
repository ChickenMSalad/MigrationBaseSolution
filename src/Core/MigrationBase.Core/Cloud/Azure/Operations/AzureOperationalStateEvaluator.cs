namespace MigrationBase.Core.Cloud.Azure.Operations;

/// <summary>
/// Provides conservative default evaluation helpers for operational state descriptors.
/// </summary>
public sealed class AzureOperationalStateEvaluator
{
    public AzureOperationalStateDescriptor Evaluate(
        string environmentName,
        string deploymentRing,
        string scopeName,
        string scopeKind,
        IEnumerable<AzureOperationalStateSignal> signals)
    {
        ArgumentNullException.ThrowIfNull(signals);

        var signalList = signals.Where(signal => signal is not null).ToList();
        var hasCritical = signalList.Any(signal => !signal.IsHealthy && signal.Severity == AzureOperationalStateSeverity.Critical);
        var hasError = signalList.Any(signal => !signal.IsHealthy && signal.Severity == AzureOperationalStateSeverity.Error);
        var hasWarning = signalList.Any(signal => !signal.IsHealthy && signal.Severity == AzureOperationalStateSeverity.Warning);

        var stateKind = hasCritical
            ? AzureOperationalStateKind.Blocked
            : hasError
                ? AzureOperationalStateKind.Degraded
                : hasWarning
                    ? AzureOperationalStateKind.Degraded
                    : AzureOperationalStateKind.Ready;

        return new AzureOperationalStateDescriptor
        {
            EnvironmentName = environmentName ?? string.Empty,
            DeploymentRing = deploymentRing ?? string.Empty,
            ScopeName = scopeName ?? string.Empty,
            ScopeKind = scopeKind ?? string.Empty,
            StateKind = stateKind,
            AllowsNewWork = stateKind == AzureOperationalStateKind.Ready || stateKind == AzureOperationalStateKind.Running,
            AllowsReplay = stateKind == AzureOperationalStateKind.Ready || stateKind == AzureOperationalStateKind.Running,
            AllowsOperatorActions = stateKind != AzureOperationalStateKind.Stopped,
            Summary = CreateSummary(stateKind, signalList),
            CapturedAtUtc = DateTimeOffset.UtcNow,
            Signals = signalList
        };
    }

    private static string CreateSummary(AzureOperationalStateKind stateKind, IReadOnlyCollection<AzureOperationalStateSignal> signals)
    {
        var unhealthyCount = signals.Count(signal => !signal.IsHealthy);
        if (unhealthyCount == 0)
        {
            return "Operational state is ready.";
        }

        return $"Operational state is {stateKind} with {unhealthyCount} unhealthy signal(s).";
    }
}
