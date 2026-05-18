using Migration.Infrastructure.State.OperationalStore.Sql.Validation;
using Microsoft.Extensions.Logging;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Health;

public sealed class OperationalStoreHealthCheck
{
    private readonly IOperationalStoreSchemaValidator _schemaValidator;
    private readonly ILogger<OperationalStoreHealthCheck> _logger;

    public OperationalStoreHealthCheck(
        IOperationalStoreSchemaValidator schemaValidator,
        ILogger<OperationalStoreHealthCheck> logger)
    {
        _schemaValidator = schemaValidator;
        _logger = logger;
    }

    public async Task<bool> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _schemaValidator.ValidateAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Operational store validation failed.");

            return false;
        }
    }
}
