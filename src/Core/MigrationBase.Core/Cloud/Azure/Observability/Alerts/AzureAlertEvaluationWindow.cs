using System;

namespace MigrationBase.Core.Cloud.Azure.Observability.Alerts;

public sealed class AzureAlertEvaluationWindow
{
    public TimeSpan Lookback { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan Frequency { get; init; } = TimeSpan.FromMinutes(1);
    public int MinimumEvaluationPeriods { get; init; } = 1;
}
