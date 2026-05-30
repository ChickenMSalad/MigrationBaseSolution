/*
P10.0C real migration runtime state validator.

Expected sqlcmd variable:
  RunId

Example:
  sqlcmd -S server.database.windows.net -d db -U user -P password -v RunId="00000000-0000-0000-0000-000000000000" -i database/sql/p10/002_p10_real_migration_runtime_state_validator.sql
*/

SET NOCOUNT ON;

DECLARE @RunId uniqueidentifier = TRY_CONVERT(uniqueidentifier, '$(RunId)');

IF @RunId IS NULL
BEGIN
    THROW 51000, 'RunId sqlcmd variable is required and must be a valid uniqueidentifier.', 1;
END;

IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    THROW 51001, 'Required table migration.Runs is missing.', 1;
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    THROW 51002, 'Required table migration.WorkItems is missing.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM migration.Runs WHERE RunId = @RunId)
BEGIN
    THROW 51003, 'RunId does not exist in migration.Runs.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM migration.WorkItems WHERE RunId = @RunId)
BEGIN
    THROW 51004, 'No migration.WorkItems rows exist for RunId.', 1;
END;

SELECT
    r.RunId,
    r.Status AS RunStatus,
    r.CreatedAtUtc,
    r.UpdatedAtUtc
FROM migration.Runs r
WHERE r.RunId = @RunId;

SELECT TOP (20)
    wi.WorkItemId,
    wi.RunId,
    wi.WorkType,
    wi.Status,
    wi.AttemptCount,
    wi.ClaimedBy,
    wi.UpdatedAtUtc,
    wi.CompletedAtUtc,
    wi.LastErrorMessage
FROM migration.WorkItems wi
WHERE wi.RunId = @RunId
ORDER BY wi.WorkItemId DESC;
