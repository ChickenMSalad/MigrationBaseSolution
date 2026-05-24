/*
P7.7F SQL Operational Schema Compatibility Repair
Forward-only repair for partially-applied P7 operational schema.

Purpose:
- Do not drop or recreate the database.
- Add the missing physical tables expected by SqlOperationalRuntimeReadinessService.
- Preserve existing tables created by 001_operational_runtime_store.sql.
- Create compatibility tables by cloning existing table shapes where possible.

Run after 001 has partially/successfully created the migration schema tables, before 002/003/004 smoke scripts.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'migration')
BEGIN
    EXEC(N'CREATE SCHEMA migration');
END
GO

/* MigrationProjects: readiness requires a Projects table. Keep minimal and non-invasive. */
IF OBJECT_ID(N'[migration].[MigrationProjects]', N'U') IS NULL
BEGIN
    CREATE TABLE [migration].[MigrationProjects]
    (
        [ProjectId] uniqueidentifier NOT NULL CONSTRAINT [DF_MigrationProjects_ProjectId] DEFAULT NEWID(),
        [ProjectKey] nvarchar(200) NULL,
        [ProjectName] nvarchar(400) NULL,
        [Status] nvarchar(64) NOT NULL CONSTRAINT [DF_MigrationProjects_Status] DEFAULT N'Active',
        [SettingsJson] nvarchar(max) NULL,
        [CreatedAtUtc] datetimeoffset(7) NOT NULL CONSTRAINT [DF_MigrationProjects_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] datetimeoffset(7) NOT NULL CONSTRAINT [DF_MigrationProjects_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_MigrationProjects] PRIMARY KEY CLUSTERED ([ProjectId])
    );
END
GO

IF OBJECT_ID(N'[migration].[IX_MigrationProjects_ProjectKey]', N'IX') IS NULL
   AND COL_LENGTH(N'[migration].[MigrationProjects]', N'ProjectKey') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MigrationProjects_ProjectKey]
        ON [migration].[MigrationProjects] ([ProjectKey])
        WHERE [ProjectKey] IS NOT NULL;
END
GO

/* MigrationManifestRows: clone existing ManifestRows if present; otherwise create the shape required by run fan-out. */
IF OBJECT_ID(N'[migration].[MigrationManifestRows]', N'U') IS NULL
BEGIN
    IF OBJECT_ID(N'[migration].[ManifestRows]', N'U') IS NOT NULL
    BEGIN
        SELECT TOP (0) *
        INTO [migration].[MigrationManifestRows]
        FROM [migration].[ManifestRows];
    END
    ELSE
    BEGIN
        CREATE TABLE [migration].[MigrationManifestRows]
        (
            [ManifestRowId] uniqueidentifier NOT NULL CONSTRAINT [DF_MigrationManifestRows_ManifestRowId] DEFAULT NEWID(),
            [RunId] uniqueidentifier NOT NULL,
            [RowNumber] bigint NOT NULL,
            [Status] nvarchar(64) NOT NULL CONSTRAINT [DF_MigrationManifestRows_Status] DEFAULT N'Pending',
            [PayloadJson] nvarchar(max) NULL,
            [CreatedAtUtc] datetimeoffset(7) NOT NULL CONSTRAINT [DF_MigrationManifestRows_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
            [UpdatedAtUtc] datetimeoffset(7) NOT NULL CONSTRAINT [DF_MigrationManifestRows_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
            CONSTRAINT [PK_MigrationManifestRows] PRIMARY KEY CLUSTERED ([ManifestRowId])
        );
    END
END
GO

IF OBJECT_ID(N'[migration].[PK_MigrationManifestRows]', N'PK') IS NULL
   AND COL_LENGTH(N'[migration].[MigrationManifestRows]', N'ManifestRowId') IS NOT NULL
BEGIN
    ALTER TABLE [migration].[MigrationManifestRows]
        ADD CONSTRAINT [PK_MigrationManifestRows] PRIMARY KEY CLUSTERED ([ManifestRowId]);
END
GO

IF OBJECT_ID(N'[migration].[IX_MigrationManifestRows_RunId_Status_RowNumber]', N'IX') IS NULL
   AND COL_LENGTH(N'[migration].[MigrationManifestRows]', N'RunId') IS NOT NULL
   AND COL_LENGTH(N'[migration].[MigrationManifestRows]', N'Status') IS NOT NULL
   AND COL_LENGTH(N'[migration].[MigrationManifestRows]', N'RowNumber') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MigrationManifestRows_RunId_Status_RowNumber]
        ON [migration].[MigrationManifestRows] ([RunId], [Status], [RowNumber]);
END
GO

/* MigrationRunCheckpoints: clone existing MigrationCheckpoints if present; otherwise create a durable checkpoint table. */
IF OBJECT_ID(N'[migration].[MigrationRunCheckpoints]', N'U') IS NULL
BEGIN
    IF OBJECT_ID(N'[migration].[MigrationCheckpoints]', N'U') IS NOT NULL
    BEGIN
        SELECT TOP (0) *
        INTO [migration].[MigrationRunCheckpoints]
        FROM [migration].[MigrationCheckpoints];
    END
    ELSE
    BEGIN
        CREATE TABLE [migration].[MigrationRunCheckpoints]
        (
            [CheckpointId] uniqueidentifier NOT NULL CONSTRAINT [DF_MigrationRunCheckpoints_CheckpointId] DEFAULT NEWID(),
            [RunId] uniqueidentifier NOT NULL,
            [CheckpointName] nvarchar(200) NOT NULL,
            [CheckpointJson] nvarchar(max) NULL,
            [CreatedAtUtc] datetimeoffset(7) NOT NULL CONSTRAINT [DF_MigrationRunCheckpoints_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
            [UpdatedAtUtc] datetimeoffset(7) NOT NULL CONSTRAINT [DF_MigrationRunCheckpoints_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
            CONSTRAINT [PK_MigrationRunCheckpoints] PRIMARY KEY CLUSTERED ([CheckpointId])
        );
    END
END
GO

IF OBJECT_ID(N'[migration].[IX_MigrationRunCheckpoints_RunId]', N'IX') IS NULL
   AND COL_LENGTH(N'[migration].[MigrationRunCheckpoints]', N'RunId') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MigrationRunCheckpoints_RunId]
        ON [migration].[MigrationRunCheckpoints] ([RunId]);
END
GO

/* MigrationAssetMappings: clone existing IdentifierMappings when available; otherwise create a compact mapping table. */
IF OBJECT_ID(N'[migration].[MigrationAssetMappings]', N'U') IS NULL
BEGIN
    IF OBJECT_ID(N'[migration].[IdentifierMappings]', N'U') IS NOT NULL
    BEGIN
        SELECT TOP (0) *
        INTO [migration].[MigrationAssetMappings]
        FROM [migration].[IdentifierMappings];
    END
    ELSE IF OBJECT_ID(N'[migration].[MigrationIdentifierMaps]', N'U') IS NOT NULL
    BEGIN
        SELECT TOP (0) *
        INTO [migration].[MigrationAssetMappings]
        FROM [migration].[MigrationIdentifierMaps];
    END
    ELSE
    BEGIN
        CREATE TABLE [migration].[MigrationAssetMappings]
        (
            [MappingId] uniqueidentifier NOT NULL CONSTRAINT [DF_MigrationAssetMappings_MappingId] DEFAULT NEWID(),
            [RunId] uniqueidentifier NOT NULL,
            [SourceSystem] nvarchar(100) NULL,
            [SourceId] nvarchar(450) NOT NULL,
            [TargetSystem] nvarchar(100) NULL,
            [TargetId] nvarchar(450) NULL,
            [EntityType] nvarchar(100) NULL,
            [MappingJson] nvarchar(max) NULL,
            [CreatedAtUtc] datetimeoffset(7) NOT NULL CONSTRAINT [DF_MigrationAssetMappings_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
            [UpdatedAtUtc] datetimeoffset(7) NOT NULL CONSTRAINT [DF_MigrationAssetMappings_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
            CONSTRAINT [PK_MigrationAssetMappings] PRIMARY KEY CLUSTERED ([MappingId])
        );
    END
END
GO

IF OBJECT_ID(N'[migration].[IX_MigrationAssetMappings_RunId]', N'IX') IS NULL
   AND COL_LENGTH(N'[migration].[MigrationAssetMappings]', N'RunId') IS NOT NULL
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MigrationAssetMappings_RunId]
        ON [migration].[MigrationAssetMappings] ([RunId]);
END
GO

/* Final readiness visibility report. */
SELECT
    RequiredTable = v.TableName,
    ExistsInMigrationSchema = CONVERT(bit, CASE WHEN OBJECT_ID(N'[migration].[' + v.TableName + N']', N'U') IS NULL THEN 0 ELSE 1 END)
FROM (VALUES
    (N'MigrationProjects'),
    (N'MigrationRuns'),
    (N'MigrationManifestRows'),
    (N'MigrationWorkItems'),
    (N'MigrationFailures'),
    (N'MigrationRunCheckpoints'),
    (N'MigrationAssetMappings')
) AS v(TableName)
ORDER BY v.TableName;
GO
