namespace Migration.Infrastructure.State.OperationalStore.Sql.Validation;

public interface IOperationalStoreSchemaValidator
{
    Task ValidateAsync(
        CancellationToken cancellationToken = default);
}
