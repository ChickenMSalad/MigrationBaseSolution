namespace Migration.Admin.Api.Operational.SqlMetrics;

public interface ISqlOperationalMetricsReader
{
    Task<SqlOperationalMetricsSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken);
}
