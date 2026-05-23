using System;
using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Observability;

/// <summary>
/// Describes a runtime metric emitted by the Azure-hosted migration platform.
/// </summary>
public sealed class AzureMetricDescriptor
{
    public AzureMetricDescriptor(
        string name,
        string category,
        string unit,
        AzureMetricKind kind,
        IReadOnlyCollection<string>? dimensions = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Metric name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Metric category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Metric unit is required.", nameof(unit));

        Name = name.Trim();
        Category = category.Trim();
        Unit = unit.Trim();
        Kind = kind;
        Dimensions = dimensions ?? Array.Empty<string>();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public string Name { get; }

    public string Category { get; }

    public string Unit { get; }

    public AzureMetricKind Kind { get; }

    public IReadOnlyCollection<string> Dimensions { get; }

    public string? Description { get; }
}
