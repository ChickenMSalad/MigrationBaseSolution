using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndValidationScenario
{
    public required string ScenarioId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public bool RequiresQueueDispatch { get; init; } = true;

    public bool RequiresManifestExecution { get; init; } = true;

    public bool RequiresConnectorExecution { get; init; } = true;

    public bool RequiresFailureRuntime { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Properties { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
