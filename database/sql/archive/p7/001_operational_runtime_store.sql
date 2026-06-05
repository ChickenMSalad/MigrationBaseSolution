/*
P7.1 SQL Operational Store Foundation
MigrationBaseSolution

Purpose:
  Durable operational runtime store for enterprise-scale migration execution.

Notes:
  - SQL Server is the operational runtime store.
  - Excel/CSV remain ingestion/export artifacts only.
  - This script is additive and idempotent.
*/

SET XACT_ABORT ON;
GO

IF SCHEMA_ID(N'migration') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA migration AUTHORIZATION dbo;');
END
GO

IF OBJECT_ID(N'migration.MigrationRuns', N'U') IS NULL
BEGIN
    CREATE TABLE migration.MigrationRuns
    (
        RunId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MigrationRuns PRIMARY KEY,
        RunName NVARCHAR(256) NOT NULL,
        EnvironmentName NVARCHAR(128) NULL,
        SourceSystem NVARCHAR(128) NULL,
        TargetSystem NVARCHAR(128) NULL,
        Status NVARCHAR(64) NOT NULL,
        IsDryRun BIT NOT NULL CONSTRAINT DF_MigrationRuns_IsDryRun DEFAULT(0),
        RequestedBy NVARCHAR(256) NULL,
        RequestedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_MigrationRuns_RequestedAtUtc DEFAULT(SYSUTCDATETIME()),
        StartedAtUtc DATETIME2(3) NULL,
        CompletedAtUtc DATETIME2(3) NULL,
        CancelRequestedAtUtc DATETIME2(3) NULL,
        AbortRequestedAtUtc DATETIME2(3) NULL,
        OptionsJson NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_MigrationRuns_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_MigrationRuns_UpdatedAtUtc DEFAULT(SYSUTCDATETIME()),
        RowVersion ROWVERSION NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'migration.MigrationRuns') AND name = N'IX_MigrationRuns_Status_RequestedAtUtc')
BEGIN
    CREATE INDEX IX_MigrationRuns_Status_RequestedAtUtc
        ON migration.MigrationRuns(Status, RequestedAtUtc)
        INCLUDE (RunName, IsDryRun, StartedAtUtc, CompletedAtUtc);
END
GO

IF OBJECT_ID(N'migration.ManifestRows', N'U') IS NULL
BEGIN
    CREATE TABLE migration.ManifestRows
    (
        ManifestRowId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ManifestRows PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        SourceRowNumber BIGINT NULL,
        SourceExternalId NVARCHAR(512) NULL,
        SourcePath NVARCHAR(2048) NULL,
        ContentHash NVARCHAR(128) NULL,
        Operation NVARCHAR(64) NULL,
        ManifestStatus NVARCHAR(64) NOT NULL CONSTRAINT DF_ManifestRows_ManifestStatus DEFAULT(N'Pending'),
        PayloadJson NVARCHAR(MAX) NULL,
        ValidationJson NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_ManifestRows_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_ManifestRows_UpdatedAtUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_ManifestRows_MigrationRuns FOREIGN KEY (RunId) REFERENCES migration.MigrationRuns(RunId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'migration.ManifestRows') AND name = N'IX_ManifestRows_RunId_Status_RowId')
BEGIN
    CREATE INDEX IX_ManifestRows_RunId_Status_RowId
        ON migration.ManifestRows(RunId, ManifestStatus, ManifestRowId)
        INCLUDE (SourceExternalId, Operation, SourcePath);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'migration.ManifestRows') AND name = N'IX_ManifestRows_RunId_SourceExternalId')
BEGIN
    CREATE INDEX IX_ManifestRows_RunId_SourceExternalId
        ON migration.ManifestRows(RunId, SourceExternalId)
        WHERE SourceExternalId IS NOT NULL;
END
GO

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    CREATE TABLE migration.WorkItems
    (
        WorkItemId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkItems PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestRowId BIGINT NULL,
        WorkType NVARCHAR(128) NOT NULL,
        Status NVARCHAR(64) NOT NULL CONSTRAINT DF_WorkItems_Status DEFAULT(N'Queued'),
        Priority INT NOT NULL CONSTRAINT DF_WorkItems_Priority DEFAULT(100),
        AttemptCount INT NOT NULL CONSTRAINT DF_WorkItems_AttemptCount DEFAULT(0),
        MaxAttempts INT NOT NULL CONSTRAINT DF_WorkItems_MaxAttempts DEFAULT(5),
        AvailableAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_WorkItems_AvailableAtUtc DEFAULT(SYSUTCDATETIME()),
        ClaimedBy NVARCHAR(256) NULL,
        ClaimedAtUtc DATETIME2(3) NULL,
        LeaseExpiresAtUtc DATETIME2(3) NULL,
        StartedAtUtc DATETIME2(3) NULL,
        CompletedAtUtc DATETIME2(3) NULL,
        IdempotencyKey NVARCHAR(512) NULL,
        PayloadJson NVARCHAR(MAX) NULL,
        ResultJson NVARCHAR(MAX) NULL,
        LastErrorCode NVARCHAR(128) NULL,
        LastErrorMessage NVARCHAR(2048) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_WorkItems_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_WorkItems_UpdatedAtUtc DEFAULT(SYSUTCDATETIME()),
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_WorkItems_MigrationRuns FOREIGN KEY (RunId) REFERENCES migration.MigrationRuns(RunId),
        CONSTRAINT FK_WorkItems_ManifestRows FOREIGN KEY (ManifestRowId) REFERENCES migration.ManifestRows(ManifestRowId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'migration.WorkItems') AND name = N'IX_WorkItems_ClaimQueue')
BEGIN
    CREATE INDEX IX_WorkItems_ClaimQueue
        ON migration.WorkItems(Status, AvailableAtUtc, Priority, WorkItemId)
        INCLUDE (RunId, ManifestRowId, WorkType, AttemptCount, MaxAttempts, LeaseExpiresAtUtc)
        WHERE Status IN (N'Queued', N'RetryScheduled');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'migration.WorkItems') AND name = N'IX_WorkItems_LeaseRecovery')
BEGIN
    CREATE INDEX IX_WorkItems_LeaseRecovery
        ON migration.WorkItems(Status, LeaseExpiresAtUtc, WorkItemId)
        INCLUDE (RunId, ClaimedBy, AttemptCount)
        WHERE Status = N'Running';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'migration.WorkItems') AND name = N'UX_WorkItems_RunId_IdempotencyKey')
BEGIN
    CREATE UNIQUE INDEX UX_WorkItems_RunId_IdempotencyKey
        ON migration.WorkItems(RunId, IdempotencyKey)
        WHERE IdempotencyKey IS NOT NULL;
END
GO

IF OBJECT_ID(N'migration.WorkItemFailures', N'U') IS NULL
BEGIN
    CREATE TABLE migration.WorkItemFailures
    (
        WorkItemFailureId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkItemFailures PRIMARY KEY,
        WorkItemId BIGINT NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        AttemptNumber INT NOT NULL,
        ErrorCode NVARCHAR(128) NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ExceptionType NVARCHAR(512) NULL,
        IsRetryable BIT NOT NULL CONSTRAINT DF_WorkItemFailures_IsRetryable DEFAULT(0),
        FailureJson NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_WorkItemFailures_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_WorkItemFailures_WorkItems FOREIGN KEY (WorkItemId) REFERENCES migration.WorkItems(WorkItemId),
        CONSTRAINT FK_WorkItemFailures_MigrationRuns FOREIGN KEY (RunId) REFERENCES migration.MigrationRuns(RunId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'migration.WorkItemFailures') AND name = N'IX_WorkItemFailures_RunId_WorkItemId')
BEGIN
    CREATE INDEX IX_WorkItemFailures_RunId_WorkItemId
        ON migration.WorkItemFailures(RunId, WorkItemId, CreatedAtUtc DESC);
END
GO

IF OBJECT_ID(N'migration.IdentifierMappings', N'U') IS NULL
BEGIN
    CREATE TABLE migration.IdentifierMappings
    (
        IdentifierMappingId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IdentifierMappings PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        SourceSystem NVARCHAR(128) NOT NULL,
        TargetSystem NVARCHAR(128) NOT NULL,
        SourceIdentifier NVARCHAR(512) NOT NULL,
        TargetIdentifier NVARCHAR(512) NOT NULL,
        EntityType NVARCHAR(128) NULL,
        MappingJson NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_IdentifierMappings_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_IdentifierMappings_UpdatedAtUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_IdentifierMappings_MigrationRuns FOREIGN KEY (RunId) REFERENCES migration.MigrationRuns(RunId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'migration.IdentifierMappings') AND name = N'UX_IdentifierMappings_Run_Source_Target_Entity')
BEGIN
    CREATE UNIQUE INDEX UX_IdentifierMappings_Run_Source_Target_Entity
        ON migration.IdentifierMappings(RunId, SourceSystem, TargetSystem, SourceIdentifier, EntityType);
END
GO

IF OBJECT_ID(N'migration.ExecutionCheckpoints', N'U') IS NULL
BEGIN
    CREATE TABLE migration.ExecutionCheckpoints
    (
        ExecutionCheckpointId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExecutionCheckpoints PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        CheckpointName NVARCHAR(256) NOT NULL,
        CheckpointValue NVARCHAR(2048) NULL,
        CheckpointJson NVARCHAR(MAX) NULL,
        UpdatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_ExecutionCheckpoints_UpdatedAtUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_ExecutionCheckpoints_MigrationRuns FOREIGN KEY (RunId) REFERENCES migration.MigrationRuns(RunId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'migration.ExecutionCheckpoints') AND name = N'UX_ExecutionCheckpoints_RunId_Name')
BEGIN
    CREATE UNIQUE INDEX UX_ExecutionCheckpoints_RunId_Name
        ON migration.ExecutionCheckpoints(RunId, CheckpointName);
END
GO

IF OBJECT_ID(N'migration.WorkerHeartbeats', N'U') IS NULL
BEGIN
    CREATE TABLE migration.WorkerHeartbeats
    (
        WorkerId NVARCHAR(256) NOT NULL CONSTRAINT PK_WorkerHeartbeats PRIMARY KEY,
        HostName NVARCHAR(256) NULL,
        ProcessId INT NULL,
        RuntimeVersion NVARCHAR(128) NULL,
        LastHeartbeatUtc DATETIME2(3) NOT NULL,
        CurrentRunId UNIQUEIDENTIFIER NULL,
        Status NVARCHAR(64) NOT NULL,
        DetailsJson NVARCHAR(MAX) NULL
    );
END
GO

IF OBJECT_ID(N'migration.ReleaseEvidence', N'U') IS NULL
BEGIN
    CREATE TABLE migration.ReleaseEvidence
    (
        ReleaseEvidenceId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReleaseEvidence PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NULL,
        EvidenceType NVARCHAR(128) NOT NULL,
        EvidenceName NVARCHAR(256) NOT NULL,
        Status NVARCHAR(64) NOT NULL,
        EvidenceJson NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_ReleaseEvidence_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_ReleaseEvidence_MigrationRuns FOREIGN KEY (RunId) REFERENCES migration.MigrationRuns(RunId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'migration.ReleaseEvidence') AND name = N'IX_ReleaseEvidence_RunId_Type_Status')
BEGIN
    CREATE INDEX IX_ReleaseEvidence_RunId_Type_Status
        ON migration.ReleaseEvidence(RunId, EvidenceType, Status, CreatedAtUtc DESC);
END
GO
