namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition.Handoff;

/// <summary>
/// Records the P6.1 runtime composition surface that is ready for concrete host and worker integration.
/// </summary>
public sealed class AzureRuntimeCompositionHandoffManifest
{
    public string Name { get; init; } = "P6.1 Runtime Composition Handoff";

    public string Version { get; init; } = "p6.1";

    public string Summary { get; init; } = string.Empty;

    public IList<AzureRuntimeCompositionHandoffItem> Items { get; } = new List<AzureRuntimeCompositionHandoffItem>();

    public IList<string> NextImplementationAreas { get; } = new List<string>();
}
