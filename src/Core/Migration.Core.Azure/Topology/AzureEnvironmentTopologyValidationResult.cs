using System.Collections.Generic;
using System.Linq;

namespace Migration.Core.Azure.Topology;

public sealed class AzureEnvironmentTopologyValidationResult
{
    public AzureEnvironmentTopologyValidationResult(IEnumerable<string> errors, IEnumerable<string> warnings)
    {
        Errors = errors.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        Warnings = warnings.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }

    public IReadOnlyList<string> Errors { get; }

    public IReadOnlyList<string> Warnings { get; }

    public bool IsValid => Errors.Count == 0;

    public static AzureEnvironmentTopologyValidationResult Success { get; } = new([], []);
}
