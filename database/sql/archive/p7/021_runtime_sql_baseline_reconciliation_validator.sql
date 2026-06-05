SET NOCOUNT ON;

DECLARE @Failures TABLE
(
    FailureId int IDENTITY(1,1) NOT NULL,
    FailureMessage nvarchar(4000) NOT NULL
);

IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'Missing canonical table migration.Runs.');
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'Missing canonical table migration.WorkItems.');
END;

IF OBJECT_ID(N'migration.ManifestRows', N'U') IS NULL
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'Missing canonical table migration.ManifestRows.');
END;

IF COL_LENGTH(N'migration.WorkItems', N'WorkItemId') IS NULL
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'migration.WorkItems.WorkItemId is missing.');
END
ELSE IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns c
    JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'migration.WorkItems')
      AND c.name = N'WorkItemId'
      AND ty.name = N'bigint'
      AND c.is_identity = 1
)
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'migration.WorkItems.WorkItemId must be bigint identity.');
END;

IF COL_LENGTH(N'migration.ManifestRows', N'ManifestRowId') IS NULL
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'migration.ManifestRows.ManifestRowId is missing.');
END
ELSE IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns c
    JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'migration.ManifestRows')
      AND c.name = N'ManifestRowId'
      AND ty.name = N'bigint'
      AND c.is_identity = 1
)
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'migration.ManifestRows.ManifestRowId must be bigint identity.');
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id = OBJECT_ID(N'migration.WorkItems')
      AND fk.referenced_object_id = OBJECT_ID(N'migration.Runs')
)
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'migration.WorkItems must reference migration.Runs.');
END;

IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id = OBJECT_ID(N'migration.WorkItems')
      AND fk.referenced_object_id = OBJECT_ID(N'migration.MigrationRuns')
)
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'migration.WorkItems must not reference legacy migration.MigrationRuns.');
END;

IF EXISTS (SELECT 1 FROM @Failures)
BEGIN
    SELECT FailureId, FailureMessage FROM @Failures ORDER BY FailureId;
    THROW 51079, 'P7.9H runtime SQL baseline reconciliation validator failed.', 1;
END;

SELECT N'P7.9H runtime SQL baseline reconciliation validator passed.' AS ValidationResult;
