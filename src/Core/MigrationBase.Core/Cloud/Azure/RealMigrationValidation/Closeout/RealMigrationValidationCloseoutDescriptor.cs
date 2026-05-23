namespace MigrationBase.Core.Cloud.Azure.RealMigrationValidation.Closeout;

public sealed class RealMigrationValidationCloseoutDescriptor
{
    public string ValidationRunId { get; set; } = string.Empty;

    public string MigrationProfileKey { get; set; } = string.Empty;

    public string EnvironmentKey { get; set; } = string.Empty;

    public RealMigrationValidationCloseoutStatus Status { get; set; } = RealMigrationValidationCloseoutStatus.NotStarted;

    public IReadOnlyCollection<RealMigrationValidationCloseoutCriterion> Criteria { get; set; } = Array.Empty<RealMigrationValidationCloseoutCriterion>();

    public IReadOnlyCollection<string> BlockingReasons { get; set; } = Array.Empty<string>();

    public DateTimeOffset? CompletedAtUtc { get; set; }
}
