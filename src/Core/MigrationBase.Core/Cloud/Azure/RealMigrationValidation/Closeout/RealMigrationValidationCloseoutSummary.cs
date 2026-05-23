namespace MigrationBase.Core.Cloud.Azure.RealMigrationValidation.Closeout;

public sealed class RealMigrationValidationCloseoutSummary
{
    public string MigrationProfileKey { get; set; } = string.Empty;

    public string EnvironmentKey { get; set; } = string.Empty;

    public int RequiredCriteriaCount { get; set; }

    public int SatisfiedRequiredCriteriaCount { get; set; }

    public int BlockingIssueCount { get; set; }

    public bool IsReadyForNextPhase => BlockingIssueCount == 0 && RequiredCriteriaCount == SatisfiedRequiredCriteriaCount;
}
