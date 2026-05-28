SET NOCOUNT ON;

DECLARE @Failure TABLE
(
    FailureMessage NVARCHAR(4000) NOT NULL
);

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    INSERT INTO @Failure VALUES (N'migration.WorkItems is missing.');
END;

IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    INSERT INTO @Failure VALUES (N'migration.Runs is missing.');
END;

IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_WorkItems_MigrationRuns'
      AND parent_object_id = OBJECT_ID(N'migration.WorkItems')
)
BEGIN
    INSERT INTO @Failure VALUES (N'Legacy FK FK_WorkItems_MigrationRuns still exists.');
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
    JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
    WHERE fk.name = N'FK_WorkItems_Runs'
      AND OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'migration'
      AND OBJECT_NAME(fk.parent_object_id) = N'WorkItems'
      AND pc.name = N'RunId'
      AND OBJECT_SCHEMA_NAME(fk.referenced_object_id) = N'migration'
      AND OBJECT_NAME(fk.referenced_object_id) = N'Runs'
      AND rc.name = N'RunId'
)
BEGIN
    INSERT INTO @Failure VALUES (N'Canonical FK FK_WorkItems_Runs is missing or points to the wrong parent.');
END;

IF EXISTS
(
    SELECT 1
    FROM migration.WorkItems wi
    LEFT JOIN migration.Runs r ON r.RunId = wi.RunId
    WHERE r.RunId IS NULL
)
BEGIN
    INSERT INTO @Failure VALUES (N'At least one migration.WorkItems row has no parent in migration.Runs.');
END;

IF EXISTS (SELECT 1 FROM @Failure)
BEGIN
    SELECT FailureMessage FROM @Failure ORDER BY FailureMessage;
    THROW 51096, 'P7.9A runtime run parent FK validation failed.', 1;
END;

SELECT
    N'P7.9A runtime run parent FK validation passed.' AS Result,
    COUNT_BIG(*) AS WorkItemsChecked
FROM migration.WorkItems;
