namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationRequest
{
    public required AzureEndToEndValidationScenario Scenario { get; init; }

    public string? RequestedBy { get; init; }

    public bool TreatWarningsAsFailures { get; init; }
}
