SET NOCOUNT ON;

DECLARE @errors TABLE
(
    ErrorMessage nvarchar(4000) NOT NULL
);

IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    INSERT INTO @errors VALUES (N'migration.Runs is missing.');
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    INSERT INTO @errors VALUES (N'migration.WorkItems is missing.');
END;

IF OBJECT_ID(N'migration.ManifestRows', N'U') IS NULL
BEGIN
    INSERT INTO @errors VALUES (N'migration.ManifestRows is missing.');
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.columns c
        JOIN sys.types ty
            ON ty.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'migration.WorkItems')
          AND c.name = N'WorkItemId'
          AND ty.name = N'bigint'
          AND c.is_identity = 1
    )
    BEGIN
        INSERT INTO @errors VALUES (N'migration.WorkItems.WorkItemId must be bigint identity.');
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = OBJECT_ID(N'migration.WorkItems')
          AND fk.referenced_object_id = OBJECT_ID(N'migration.Runs')
    )
    BEGIN
        INSERT INTO @errors VALUES (N'migration.WorkItems must reference migration.Runs.');
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = OBJECT_ID(N'migration.WorkItems')
          AND fk.referenced_object_id = OBJECT_ID(N'migration.MigrationRuns')
    )
    BEGIN
        INSERT INTO @errors VALUES (N'migration.WorkItems still references legacy migration.MigrationRuns.');
    END;
END;

IF OBJECT_ID(N'migration.ManifestRows', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.columns c
        JOIN sys.types ty
            ON ty.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'migration.ManifestRows')
          AND c.name = N'ManifestRowId'
          AND ty.name = N'bigint'
          AND c.is_identity = 1
    )
    BEGIN
        INSERT INTO @errors VALUES (N'migration.ManifestRows.ManifestRowId must be bigint identity.');
    END;
END;

IF EXISTS (SELECT 1 FROM @errors)
BEGIN
    SELECT ErrorMessage FROM @errors ORDER BY ErrorMessage;
    THROW 51010, 'P7.10B canonical SQL baseline handoff validation failed.', 1;
END;

SELECT 'P7.10B canonical SQL baseline handoff validation passed.' AS ValidationResult;
