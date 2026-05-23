using System;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition.Binding;

public sealed class AzureRuntimeCompositionBinding
{
    public string Key { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public AzureRuntimeCompositionBindingKind Kind { get; init; }

    public AzureRuntimeCompositionBindingRequirement Requirement { get; init; }

    public string ConfigurationSection { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool AppliesToRole(string hostRole)
    {
        return !string.IsNullOrWhiteSpace(hostRole);
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Key)
            && !string.IsNullOrWhiteSpace(DisplayName)
            && Enum.IsDefined(typeof(AzureRuntimeCompositionBindingKind), Kind)
            && Enum.IsDefined(typeof(AzureRuntimeCompositionBindingRequirement), Requirement);
    }
}
