using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Observability;

public interface IAzureMetricRegistry
{
    IReadOnlyCollection<AzureMetricDescriptor> Metrics { get; }

    bool TryGetMetric(string name, out AzureMetricDescriptor? descriptor);
}
