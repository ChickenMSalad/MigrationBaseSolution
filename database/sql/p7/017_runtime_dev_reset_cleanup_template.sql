/*
P7.9E guarded dev reset cleanup template.

This script is intentionally guarded. It performs no destructive action unless
@AllowDestructiveReset is changed to 1 by the operator after reviewing diagnostics.

This is for DEV cloud only. Do not run against production.
*/

SET NOCOUNT ON;

DECLARE @AllowDestructiveReset bit = 0;

IF @AllowDestructiveReset <> 1
BEGIN
    THROW 51090, 'Destructive dev reset is disabled. Review diagnostics, then set @AllowDestructiveReset = 1 intentionally.', 1;
END;

BEGIN TRANSACTION;

/* Delete child/detail runtime rows before parent work items. */
IF OBJECT_ID(N'migration.WorkItemExecutionAttempts', N'U') IS NOT NULL
BEGIN
    DELETE FROM migration.WorkItemExecutionAttempts;
END;

IF OBJECT_ID(N'migration.WorkItemFailures', N'U') IS NOT NULL
BEGIN
    DELETE FROM migration.WorkItemFailures;
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NOT NULL
BEGIN
    DELETE FROM migration.WorkItems;
END;

IF OBJECT_ID(N'migration.ManifestRows', N'U') IS NOT NULL
BEGIN
    DELETE FROM migration.ManifestRows;
END;

/* Runs are cleared only for dev reset after work/manifest rows are gone. */
IF OBJECT_ID(N'migration.Runs', N'U') IS NOT NULL
BEGIN
    DELETE FROM migration.Runs;
END;

IF OBJECT_ID(N'migration.MigrationRuns', N'U') IS NOT NULL
BEGIN
    DELETE FROM migration.MigrationRuns;
END;

COMMIT TRANSACTION;

SELECT 'Dev runtime reset cleanup completed.' AS Message;
