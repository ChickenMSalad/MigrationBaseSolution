using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionValidationResult
{
    public IReadOnlyList<AzureConnectorExecutionValidationIssue> Issues { get; init; } =
        new List<AzureConnectorExecutionValidationIssue>();

    public bool IsValid =>
        !Issues.Any(issue => issue.Severity == AzureConnectorExecutionValidationSeverity.Error);

    public static AzureConnectorExecutionValidationResult Success { get; } = new();
}
