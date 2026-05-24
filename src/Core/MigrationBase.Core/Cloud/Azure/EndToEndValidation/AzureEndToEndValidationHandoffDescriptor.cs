using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationHandoffDescriptor
{
    public string Area { get; init; } = "P6.8 End-to-End Runtime Validation";

    public string NextArea { get; init; } = "P6.9 Production Hardening";

    public IReadOnlyList<string> CompletedCapabilities { get; init; } =
        new[]
        {
            "end-to-end validation scenario model",
            "scenario validation runner",
            "dry-run execution harness",
            "dry-run step reporting",
            "evidence report builder",
            "end-to-end validation readiness evaluation"
        };

    public IReadOnlyList<string> HandoffExpectations { get; init; } =
        new[]
        {
            "production hardening should convert validation reports into release gates",
            "real infrastructure validation should replace synthetic dry-run probes before go-live",
            "dry-run harness should remain safe to execute without mutating source or target systems",
            "evidence reports should become required deployment artifacts"
        };
}
