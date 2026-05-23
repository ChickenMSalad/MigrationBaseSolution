namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

/// <summary>
/// Describes a prerequisite that must be satisfied before a runtime composition module is enabled.
/// </summary>
public sealed class AzureRuntimeCompositionRequirement
{
    public AzureRuntimeCompositionRequirement()
    {
    }

    public AzureRuntimeCompositionRequirement(string key, string description, bool isRequired = true)
    {
        Key = key;
        Description = description;
        IsRequired = isRequired;
    }

    public string Key { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;
}
