namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public sealed record AzureRuntimeCompositionValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static AzureRuntimeCompositionValidationResult Valid(params string[] warnings) =>
        new() { Warnings = warnings ?? Array.Empty<string>() };

    public static AzureRuntimeCompositionValidationResult Invalid(IEnumerable<string> errors, IEnumerable<string>? warnings = null) =>
        new()
        {
            Errors = errors?.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray() ?? Array.Empty<string>(),
            Warnings = warnings?.Where(warning => !string.IsNullOrWhiteSpace(warning)).ToArray() ?? Array.Empty<string>()
        };
}
