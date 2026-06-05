SET NOCOUNT ON;

/*
    P7.10B canonical runtime SQL baseline handoff inventory.

    This script is read-only. It reports whether the active runtime contract is present
    and whether legacy compatibility objects still exist.
*/

;WITH RuntimeObjects AS
(
    SELECT
        ExpectedSchemaName = CAST(v.ExpectedSchemaName AS sysname),
        ExpectedObjectName = CAST(v.ExpectedObjectName AS sysname),
        ObjectCategory = CAST(v.ObjectCategory AS nvarchar(64)),
        CanonicalRuntimeObject = CAST(v.CanonicalRuntimeObject AS bit),
        LegacyCompatibilityObject = CAST(v.LegacyCompatibilityObject AS bit)
    FROM (VALUES
        ('migration', 'Runs', 'RunParent', 1, 0),
        ('migration', 'WorkItems', 'WorkQueue', 1, 0),
        ('migration', 'ManifestRows', 'ManifestRows', 1, 0),
        ('migration', 'MigrationRuns', 'LegacyRunParent', 0, 1),
        ('migration', 'MigrationWorkItems', 'LegacyWorkQueue', 0, 1),
        ('migration', 'MigrationManifestRecords', 'LegacyManifestRecords', 0, 1)
    ) v(ExpectedSchemaName, ExpectedObjectName, ObjectCategory, CanonicalRuntimeObject, LegacyCompatibilityObject)
)
SELECT
    r.ExpectedSchemaName AS SchemaName,
    r.ExpectedObjectName AS ObjectName,
    r.ObjectCategory,
    r.CanonicalRuntimeObject,
    r.LegacyCompatibilityObject,
    ObjectExists = CONVERT(bit, CASE WHEN t.object_id IS NULL THEN 0 ELSE 1 END)
FROM RuntimeObjects r
LEFT JOIN sys.schemas s
    ON s.name = r.ExpectedSchemaName
LEFT JOIN sys.tables t
    ON t.schema_id = s.schema_id
   AND t.name = r.ExpectedObjectName
ORDER BY
    r.CanonicalRuntimeObject DESC,
    r.LegacyCompatibilityObject DESC,
    r.ExpectedObjectName;

SELECT
    ForeignKeyName = fk.name,
    ChildSchema = OBJECT_SCHEMA_NAME(fk.parent_object_id),
    ChildTable = OBJECT_NAME(fk.parent_object_id),
    ParentSchema = OBJECT_SCHEMA_NAME(fk.referenced_object_id),
    ParentTable = OBJECT_NAME(fk.referenced_object_id)
FROM sys.foreign_keys fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = 'migration'
  AND OBJECT_NAME(fk.parent_object_id) = 'WorkItems'
ORDER BY fk.name;

SELECT
    TableName = t.name,
    ColumnName = c.name,
    DataType = ty.name,
    c.is_identity,
    c.is_nullable
FROM sys.tables t
JOIN sys.schemas s
    ON s.schema_id = t.schema_id
JOIN sys.columns c
    ON c.object_id = t.object_id
JOIN sys.types ty
    ON ty.user_type_id = c.user_type_id
WHERE s.name = 'migration'
  AND t.name IN ('Runs', 'WorkItems', 'ManifestRows')
  AND c.name IN ('RunId', 'WorkItemId', 'ManifestRowId')
ORDER BY t.name, c.column_id;
