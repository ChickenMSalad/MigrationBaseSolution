using System;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationBase.Core.Cloud.Azure.ConnectorExecution;

public sealed class AzureConnectorExecutionPreflight : IAzureConnectorExecutionPreflight
{
    private readonly IAzureConnectorExecutionValidator validator;

    public AzureConnectorExecutionPreflight(
        IAzureConnectorExecutionValidator validator)
    {
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public Task<AzureConnectorExecutionPreflightResult> EvaluateAsync(
        AzureConnectorExecutionPreflightRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ExecutionRequest);

        cancellationToken.ThrowIfCancellationRequested();

        var validation = validator.Validate(
            request.ExecutionRequest,
            request.ValidationOptions);

        return Task.FromResult(
            new AzureConnectorExecutionPreflightResult
            {
                Validation = validation
            });
    }
}
