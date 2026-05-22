namespace Migration.Admin.Api.OperationalStore;

public interface IOperationalSqlSchemaSmokeTestService
{
    Task<OperationalSqlSchemaSmokeTestResult> ExecuteAsync(
        CancellationToken cancellationToken = default);
}
