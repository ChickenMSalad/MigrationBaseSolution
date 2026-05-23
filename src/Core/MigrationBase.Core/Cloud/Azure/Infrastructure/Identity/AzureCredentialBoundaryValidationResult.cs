namespace MigrationBase.Core.Cloud.Azure.Infrastructure.Identity;

public sealed class AzureCredentialBoundaryValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public IList<string> Errors { get; } = new List<string>();

    public IList<string> Warnings { get; } = new List<string>();
}
