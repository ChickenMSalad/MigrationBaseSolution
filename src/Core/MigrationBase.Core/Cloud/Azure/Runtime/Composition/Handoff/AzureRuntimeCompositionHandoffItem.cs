namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition.Handoff;

/// <summary>
/// Describes one completed runtime composition capability that can be handed off to the next P6 implementation slice.
/// </summary>
public sealed class AzureRuntimeCompositionHandoffItem
{
    public string Area { get; init; } = string.Empty;

    public string Capability { get; init; } = string.Empty;

    public string OwningHostRole { get; init; } = string.Empty;

    public bool RequiresProgramComposition { get; init; }

    public string Notes { get; init; } = string.Empty;
}
