using System;

namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public sealed class AzureEndToEndEvidenceEntry
{
    public required string Key { get; init; }

    public required string Value { get; init; }

    public string? Source { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
