namespace MigrationBase.Core.Cloud.Azure.RealMigrationValidation.Closeout;

public sealed class RealMigrationValidationCloseoutCriterion
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;

    public IReadOnlyCollection<string> EvidenceKeys { get; set; } = Array.Empty<string>();
}
