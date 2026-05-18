namespace Migration.Infrastructure.State.OperationalStore.Sql.Stores.Queries;

internal static class RunStoreSql
{
    public const string GetById = "SELECT * FROM MigrationRuns WHERE RunId = @RunId";

    public const string Insert = "/* TODO */";

    public const string MarkStarted = "/* TODO */";

    public const string MarkCompleted = "/* TODO */";

    public const string MarkFailed = "/* TODO */";
}
