namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public sealed class AzureRuntimeCompositionValidationReport
{
    private readonly List<AzureRuntimeCompositionValidationFinding> _findings = new();

    public required string PlanName { get; init; }

    public string? EnvironmentName { get; init; }

    public IReadOnlyList<AzureRuntimeCompositionValidationFinding> Findings => _findings;

    public bool HasErrors => _findings.Any(f =>
        f.Severity == AzureRuntimeCompositionValidationSeverity.Error ||
        f.Severity == AzureRuntimeCompositionValidationSeverity.Error);

    public void AddFinding(AzureRuntimeCompositionValidationFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);
        _findings.Add(finding);
    }

    public void AddFindings(IEnumerable<AzureRuntimeCompositionValidationFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        foreach (var finding in findings)
        {
            AddFinding(finding);
        }
    }
}

