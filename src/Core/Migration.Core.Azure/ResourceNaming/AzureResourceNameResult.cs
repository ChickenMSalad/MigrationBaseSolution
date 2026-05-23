namespace Migration.Core.Azure.ResourceNaming;

public sealed class AzureResourceNameResult
{
    public AzureResourceNameResult(string value, IReadOnlyList<string> segments, IReadOnlyList<string> warnings)
    {
        Value = value;
        Segments = segments;
        Warnings = warnings;
    }

    public string Value { get; }

    public IReadOnlyList<string> Segments { get; }

    public IReadOnlyList<string> Warnings { get; }

    public bool HasWarnings => Warnings.Count > 0;
}
