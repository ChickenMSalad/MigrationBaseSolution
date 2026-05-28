SET NOCOUNT ON;

PRINT 'P7.9H runtime SQL baseline reconciliation plan';

;WITH ObjectStatus AS
(
    SELECT
        v.ObjectName,
        v.ExpectedCategory,
        CASE WHEN OBJECT_ID(N'migration.' + v.ObjectName, N'U') IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS ObjectExists
    FROM (VALUES
        (N'Runs', N'CanonicalRuntime'),
        (N'WorkItems', N'CanonicalRuntime'),
        (N'ManifestRows', N'CanonicalRuntime'),
        (N'MigrationRuns', N'LegacyRuntime'),
        (N'MigrationWorkItems', N'LegacyRuntime'),
        (N'MigrationManifestRows', N'LegacyRuntime'),
        (N'MigrationManifestRecords', N'LegacyRuntime')
    ) AS v(ObjectName, ExpectedCategory)
)
SELECT
    ObjectName,
    ExpectedCategory,
    ObjectExists,
    CASE
        WHEN ExpectedCategory = N'CanonicalRuntime' AND ObjectExists = 0 THEN N'Create or restore canonical runtime object before deployment.'
        WHEN ExpectedCategory = N'LegacyRuntime' AND ObjectExists = 1 THEN N'Quarantine only; do not allow active runtime FK/code/settings dependencies.'
        WHEN ExpectedCategory = N'LegacyRuntime' AND ObjectExists = 0 THEN N'Already absent; keep references blocked by code/config validators.'
        ELSE N'OK'
    END AS RecommendedAction
FROM ObjectStatus
ORDER BY ExpectedCategory, ObjectName;

SELECT
    N'WorkItems.RunId foreign key' AS CheckName,
    CASE
        WHEN EXISTS (
            SELECT 1 FROM sys.foreign_keys fk
            WHERE fk.parent_object_id = OBJECT_ID(N'migration.WorkItems')
              AND fk.referenced_object_id = OBJECT_ID(N'migration.Runs')
        ) THEN N'OK'
        WHEN EXISTS (
            SELECT 1 FROM sys.foreign_keys fk
            WHERE fk.parent_object_id = OBJECT_ID(N'migration.WorkItems')
              AND fk.referenced_object_id = OBJECT_ID(N'migration.MigrationRuns')
        ) THEN N'Needs canonicalization: currently references migration.MigrationRuns.'
        ELSE N'Missing or unknown WorkItems RunId FK; inspect before deploy.'
    END AS Status;
