SET NOCOUNT ON;

DECLARE @Failures TABLE
(
    FailureId int IDENTITY(1,1) NOT NULL,
    FailureMessage nvarchar(4000) NOT NULL
);

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    INSERT INTO @Failures (FailureMessage) VALUES (N'migration.WorkItems is missing.');
END;

IF OBJECT_ID(N'migration.ManifestRows', N'U') IS NULL
BEGIN
    INSERT INTO @Failures (FailureMessage) VALUES (N'migration.ManifestRows is missing.');
END;

IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    INSERT INTO @Failures (FailureMessage) VALUES (N'migration.Runs is missing.');
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.columns c
        JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'migration.WorkItems')
          AND c.name = N'WorkItemId'
          AND ty.name = N'bigint'
          AND c.is_identity = 1
    )
    BEGIN
        INSERT INTO @Failures (FailureMessage) VALUES (N'migration.WorkItems.WorkItemId must be bigint identity.');
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.columns c
        JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'migration.WorkItems')
          AND c.name = N'ManifestRowId'
          AND ty.name = N'bigint'
    )
    BEGIN
        INSERT INTO @Failures (FailureMessage) VALUES (N'migration.WorkItems.ManifestRowId must be bigint.');
    END;
END;

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id = OBJECT_ID(N'migration.WorkItems')
      AND fk.referenced_object_id = OBJECT_ID(N'migration.MigrationRuns')
)
BEGIN
    INSERT INTO @Failures (FailureMessage) VALUES (N'migration.WorkItems still references migration.MigrationRuns.');
END;

IF OBJECT_ID(N'migration.Runs', N'U') IS NOT NULL
AND OBJECT_ID(N'migration.WorkItems', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id = OBJECT_ID(N'migration.WorkItems')
      AND fk.referenced_object_id = OBJECT_ID(N'migration.Runs')
)
BEGIN
    INSERT INTO @Failures (FailureMessage) VALUES (N'migration.WorkItems must reference migration.Runs.');
END;

IF EXISTS (SELECT 1 FROM @Failures)
BEGIN
    SELECT FailureId, FailureMessage FROM @Failures ORDER BY FailureId;
    THROW 51079, 'Clean dev runtime post-rebuild validation failed.', 1;
END;

SELECT N'Clean dev runtime post-rebuild validation passed.' AS ValidationResult;
