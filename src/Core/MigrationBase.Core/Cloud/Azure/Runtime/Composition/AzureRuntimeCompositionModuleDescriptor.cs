namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

/// <summary>
/// Describes a composable runtime module without forcing a specific host implementation.
/// </summary>
public sealed class AzureRuntimeCompositionModuleDescriptor
{
    public string ModuleName { get; set; } = string.Empty;

    public string ModuleKey { get; set; } = string.Empty;

    public AzureRuntimeCompositionStage Stage { get; set; } = AzureRuntimeCompositionStage.Foundation;

    public int Order { get; set; }

    public bool EnabledByDefault { get; set; } = true;

    public string Description { get; set; } = string.Empty;

    public IList<string> DependsOnModuleKeys { get; } = new List<string>();

    public IList<AzureRuntimeCompositionRequirement> Requirements { get; } = new List<AzureRuntimeCompositionRequirement>();

    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
