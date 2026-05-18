/*
    P3 Set 003 — SQL Operational Store Schema

    Purpose:
    - Establish SQL Server as the durable operational truth for migration runs,
      manifest rows, work items, identifier mappings, failures, and checkpoints.
    - Blob Storage remains responsible for binaries/artifacts.
    - Queue infrastructure coordinates execution only; it is not the source of truth.

    This script is intentionally standalone and does not require EF, Dapper,
    worker changes, endpoint changes, or DI changes.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF SCHEMA_ID(N'migration') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA migration');
END;
GO

IF OBJECT_ID(N'migration.MigrationRuns', N'U') IS NULL
BEGIN
    CREATE TABLE migration.MigrationRuns
    (
        RunId UNIQUEIDENTIFIER NOT NULL,
        SourceSystem NVARCHAR(200) NOT NULL,
        TargetSystem NVARCHAR(200) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL,
        StartedAt DATETIMEOFFSET(7) NULL,
        CompletedAt DATETIMEOFFSET(7) NULL,
        FailedAt DATETIMEOFFSET(7) NULL,
        FailureReason NVARCHAR(MAX) NULL,

        CONSTRAINT PK_MigrationRuns PRIMARY KEY CLUSTERED (RunId),
        CONSTRAINT CK_MigrationRuns_Status_NotEmpty CHECK (LEN(LTRIM(RTRIM(Status))) > 0),
        CONSTRAINT CK_MigrationRuns_SourceSystem_NotEmpty CHECK (LEN(LTRIM(RTRIM(SourceSystem))) > 0),
        CONSTRAINT CK_MigrationRuns_TargetSystem_NotEmpty CHECK (LEN(LTRIM(RTRIM(TargetSystem))) > 0)
    );
END;
GO

IF OBJECT_ID(N'migration.MigrationManifestRecords', N'U') IS NULL
BEGIN
    CREATE TABLE migration.MigrationManifestRecords
    (
        ManifestRecordId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        SequenceNumber BIGINT NOT NULL,
        SourceId NVARCHAR(450) NOT NULL,
        SourcePath NVARCHAR(2048) NULL,
        SourceName NVARCHAR(512) NULL,
        ContentType NVARCHAR(255) NULL,
        ContentLength BIGINT NULL,
        Status NVARCHAR(50) NOT NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL,
        UpdatedAt DATETIMEOFFSET(7) NULL,

        CONSTRAINT PK_MigrationManifestRecords PRIMARY KEY CLUSTERED (ManifestRecordId),
        CONSTRAINT FK_MigrationManifestRecords_MigrationRuns FOREIGN KEY (RunId)
            REFERENCES migration.MigrationRuns (RunId),
        CONSTRAINT UQ_MigrationManifestRecords_RunId_SequenceNumber UNIQUE (RunId, SequenceNumber),
        CONSTRAINT UQ_MigrationManifestRecords_RunId_SourceId UNIQUE (RunId, SourceId),
        CONSTRAINT CK_MigrationManifestRecords_SequenceNumber_Positive CHECK (SequenceNumber > 0),
        CONSTRAINT CK_MigrationManifestRecords_ContentLength_NonNegative CHECK (ContentLength IS NULL OR ContentLength >= 0),
        CONSTRAINT CK_MigrationManifestRecords_SourceId_NotEmpty CHECK (LEN(LTRIM(RTRIM(SourceId))) > 0),
        CONSTRAINT CK_MigrationManifestRecords_Status_NotEmpty CHECK (LEN(LTRIM(RTRIM(Status))) > 0)
    );
END;
GO

IF OBJECT_ID(N'migration.MigrationWorkItems', N'U') IS NULL
BEGIN
    CREATE TABLE migration.MigrationWorkItems
    (
        WorkItemId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestRecordId UNIQUEIDENTIFIER NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        AttemptCount INT NOT NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL,
        LockedAt DATETIMEOFFSET(7) NULL,
        LockedBy NVARCHAR(200) NULL,
        CompletedAt DATETIMEOFFSET(7) NULL,
        FailedAt DATETIMEOFFSET(7) NULL,
        LastFailureReason NVARCHAR(MAX) NULL,

        CONSTRAINT PK_MigrationWorkItems PRIMARY KEY CLUSTERED (WorkItemId),
        CONSTRAINT FK_MigrationWorkItems_MigrationRuns FOREIGN KEY (RunId)
            REFERENCES migration.MigrationRuns (RunId),
        CONSTRAINT FK_MigrationWorkItems_MigrationManifestRecords FOREIGN KEY (ManifestRecordId)
            REFERENCES migration.MigrationManifestRecords (ManifestRecordId),
        CONSTRAINT UQ_MigrationWorkItems_ManifestRecordId UNIQUE (ManifestRecordId),
        CONSTRAINT CK_MigrationWorkItems_AttemptCount_NonNegative CHECK (AttemptCount >= 0),
        CONSTRAINT CK_MigrationWorkItems_Status_NotEmpty CHECK (LEN(LTRIM(RTRIM(Status))) > 0)
    );
END;
GO

IF OBJECT_ID(N'migration.MigrationIdentifierMaps', N'U') IS NULL
BEGIN
    CREATE TABLE migration.MigrationIdentifierMaps
    (
        IdentifierMapId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestRecordId UNIQUEIDENTIFIER NOT NULL,
        SourceId NVARCHAR(450) NOT NULL,
        TargetId NVARCHAR(450) NOT NULL,
        TargetPath NVARCHAR(2048) NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL,

        CONSTRAINT PK_MigrationIdentifierMaps PRIMARY KEY CLUSTERED (IdentifierMapId),
        CONSTRAINT FK_MigrationIdentifierMaps_MigrationRuns FOREIGN KEY (RunId)
            REFERENCES migration.MigrationRuns (RunId),
        CONSTRAINT FK_MigrationIdentifierMaps_MigrationManifestRecords FOREIGN KEY (ManifestRecordId)
            REFERENCES migration.MigrationManifestRecords (ManifestRecordId),
        CONSTRAINT UQ_MigrationIdentifierMaps_RunId_SourceId UNIQUE (RunId, SourceId),
        CONSTRAINT UQ_MigrationIdentifierMaps_ManifestRecordId UNIQUE (ManifestRecordId),
        CONSTRAINT CK_MigrationIdentifierMaps_SourceId_NotEmpty CHECK (LEN(LTRIM(RTRIM(SourceId))) > 0),
        CONSTRAINT CK_MigrationIdentifierMaps_TargetId_NotEmpty CHECK (LEN(LTRIM(RTRIM(TargetId))) > 0)
    );
END;
GO

IF OBJECT_ID(N'migration.MigrationFailures', N'U') IS NULL
BEGIN
    CREATE TABLE migration.MigrationFailures
    (
        FailureId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestRecordId UNIQUEIDENTIFIER NULL,
        WorkItemId UNIQUEIDENTIFIER NULL,
        FailureType NVARCHAR(100) NOT NULL,
        Message NVARCHAR(MAX) NOT NULL,
        Details NVARCHAR(MAX) NULL,
        IsRetriable BIT NOT NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL,

        CONSTRAINT PK_MigrationFailures PRIMARY KEY CLUSTERED (FailureId),
        CONSTRAINT FK_MigrationFailures_MigrationRuns FOREIGN KEY (RunId)
            REFERENCES migration.MigrationRuns (RunId),
        CONSTRAINT FK_MigrationFailures_MigrationManifestRecords FOREIGN KEY (ManifestRecordId)
            REFERENCES migration.MigrationManifestRecords (ManifestRecordId),
        CONSTRAINT FK_MigrationFailures_MigrationWorkItems FOREIGN KEY (WorkItemId)
            REFERENCES migration.MigrationWorkItems (WorkItemId),
        CONSTRAINT CK_MigrationFailures_FailureType_NotEmpty CHECK (LEN(LTRIM(RTRIM(FailureType))) > 0),
        CONSTRAINT CK_MigrationFailures_Message_NotEmpty CHECK (LEN(LTRIM(RTRIM(Message))) > 0)
    );
END;
GO

IF OBJECT_ID(N'migration.MigrationCheckpoints', N'U') IS NULL
BEGIN
    CREATE TABLE migration.MigrationCheckpoints
    (
        CheckpointId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        CheckpointName NVARCHAR(200) NOT NULL,
        CheckpointValue NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIMEOFFSET(7) NOT NULL,
        UpdatedAt DATETIMEOFFSET(7) NOT NULL,

        CONSTRAINT PK_MigrationCheckpoints PRIMARY KEY CLUSTERED (CheckpointId),
        CONSTRAINT FK_MigrationCheckpoints_MigrationRuns FOREIGN KEY (RunId)
            REFERENCES migration.MigrationRuns (RunId),
        CONSTRAINT UQ_MigrationCheckpoints_RunId_CheckpointName UNIQUE (RunId, CheckpointName),
        CONSTRAINT CK_MigrationCheckpoints_CheckpointName_NotEmpty CHECK (LEN(LTRIM(RTRIM(CheckpointName))) > 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MigrationRuns_Status_CreatedAt' AND object_id = OBJECT_ID(N'migration.MigrationRuns'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_MigrationRuns_Status_CreatedAt
        ON migration.MigrationRuns (Status, CreatedAt DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MigrationManifestRecords_RunId_Status_SequenceNumber' AND object_id = OBJECT_ID(N'migration.MigrationManifestRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_MigrationManifestRecords_RunId_Status_SequenceNumber
        ON migration.MigrationManifestRecords (RunId, Status, SequenceNumber)
        INCLUDE (SourceId, SourcePath, SourceName, ContentType, ContentLength, UpdatedAt);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MigrationWorkItems_RunId_Status_CreatedAt' AND object_id = OBJECT_ID(N'migration.MigrationWorkItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_MigrationWorkItems_RunId_Status_CreatedAt
        ON migration.MigrationWorkItems (RunId, Status, CreatedAt)
        INCLUDE (ManifestRecordId, AttemptCount, LockedAt, LockedBy, CompletedAt, FailedAt);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MigrationWorkItems_Status_LockedAt' AND object_id = OBJECT_ID(N'migration.MigrationWorkItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_MigrationWorkItems_Status_LockedAt
        ON migration.MigrationWorkItems (Status, LockedAt)
        INCLUDE (RunId, ManifestRecordId, AttemptCount, CreatedAt);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MigrationFailures_RunId_CreatedAt' AND object_id = OBJECT_ID(N'migration.MigrationFailures'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_MigrationFailures_RunId_CreatedAt
        ON migration.MigrationFailures (RunId, CreatedAt DESC)
        INCLUDE (ManifestRecordId, WorkItemId, FailureType, IsRetriable);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MigrationFailures_ManifestRecordId_CreatedAt' AND object_id = OBJECT_ID(N'migration.MigrationFailures'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_MigrationFailures_ManifestRecordId_CreatedAt
        ON migration.MigrationFailures (ManifestRecordId, CreatedAt DESC)
        WHERE ManifestRecordId IS NOT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MigrationFailures_WorkItemId_CreatedAt' AND object_id = OBJECT_ID(N'migration.MigrationFailures'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_MigrationFailures_WorkItemId_CreatedAt
        ON migration.MigrationFailures (WorkItemId, CreatedAt DESC)
        WHERE WorkItemId IS NOT NULL;
END;
GO
