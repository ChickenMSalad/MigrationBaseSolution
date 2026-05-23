namespace MigrationBase.Core.Cloud.Azure.Workers.Concurrency;

public sealed class AzureWorkerConcurrencyAdmissionDecision
{
    public bool IsAdmitted { get; init; }
    public string DecisionCode { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string ProfileName { get; init; } = string.Empty;
    public IReadOnlyCollection<AzureWorkerConcurrencyLimit> AppliedLimits { get; init; } = Array.Empty<AzureWorkerConcurrencyLimit>();

    public static AzureWorkerConcurrencyAdmissionDecision Admit(string profileName) => new()
    {
        IsAdmitted = true,
        DecisionCode = "Admitted",
        ProfileName = profileName
    };

    public static AzureWorkerConcurrencyAdmissionDecision Reject(string profileName, string code, string reason, IEnumerable<AzureWorkerConcurrencyLimit>? limits = null) => new()
    {
        IsAdmitted = false,
        DecisionCode = code,
        Reason = reason,
        ProfileName = profileName,
        AppliedLimits = limits?.ToArray() ?? Array.Empty<AzureWorkerConcurrencyLimit>()
    };
}
