SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF SCHEMA_ID(N'migration') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA migration');
END
GO

/*
    P7.8B SQL operational runtime bootstrap/compatibility script.

    Purpose:
    - Align the SQL operational store with the current P7 QueueExecutor/runtime contracts.
    - Preserve existing migration tables and data.
    - Avoid destructive drops except replacing migration.Runs when it is a compatibility VIEW.
    - Use migration.Runs as the coordinator table expected by SqlOperationalRunCoordinator.
    - Use migration.WorkItems / migration.ManifestRows as the queue tables expected by SqlOperationalWorkItemQueue.
*/

IF OBJECT_ID(N'migration.Runs', N'V') IS NOT NULL
BEGIN
    DROP VIEW migration.Runs;
END
GO

IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    CREATE TABLE migration.Runs
    (
        RunId uniqueidentifier NOT NULL CONSTRAINT PK_Runs PRIMARY KEY,
        ProjectId uniqueidentifier NULL,
        RunKey nvarchar(128) NULL,
        RunName nvarchar(256) NULL,
        SourceSystem nvarchar(200) NOT NULL,
        TargetSystem nvarchar(200) NOT NULL,
        Status nvarchar(50) NOT NULL,
        StatusReason nvarchar(2048) NULL,
        EnvironmentName nvarchar(128) NULL,
        IsDryRun bit NOT NULL CONSTRAINT DF_Runs_IsDryRun DEFAULT (0),
        CoordinatorOwner nvarchar(256) NULL,
        CoordinationLeaseExpiresUtc datetimeoffset(7) NULL,
        RequestedAtUtc datetimeoffset(7) NULL,
        StartedAtUtc datetimeoffset(7) NULL,
        CompletedAtUtc datetimeoffset(7) NULL,
        RequestedCancellationUtc datetimeoffset(7) NULL,
        CancellationReason nvarchar(2048) NULL,
        CompletionEvaluatedUtc datetimeoffset(7) NULL,
        CreatedAtUtc datetimeoffset(7) NOT NULL CONSTRAINT DF_Runs_CreatedAtUtc DEFAULT (sysdatetimeoffset()),
        UpdatedAtUtc datetimeoffset(7) NULL
    );
END
GO

IF COL_LENGTH(N'migration.Runs', N'ProjectId') IS NULL ALTER TABLE migration.Runs ADD ProjectId uniqueidentifier NULL;
IF COL_LENGTH(N'migration.Runs', N'RunKey') IS NULL ALTER TABLE migration.Runs ADD RunKey nvarchar(128) NULL;
IF COL_LENGTH(N'migration.Runs', N'RunName') IS NULL ALTER TABLE migration.Runs ADD RunName nvarchar(256) NULL;
IF COL_LENGTH(N'migration.Runs', N'SourceSystem') IS NULL ALTER TABLE migration.Runs ADD SourceSystem nvarchar(200) NOT NULL CONSTRAINT DF_Runs_SourceSystem DEFAULT (N'UnknownSource');
IF COL_LENGTH(N'migration.Runs', N'TargetSystem') IS NULL ALTER TABLE migration.Runs ADD TargetSystem nvarchar(200) NOT NULL CONSTRAINT DF_Runs_TargetSystem DEFAULT (N'UnknownTarget');
IF COL_LENGTH(N'migration.Runs', N'Status') IS NULL ALTER TABLE migration.Runs ADD Status nvarchar(50) NOT NULL CONSTRAINT DF_Runs_Status DEFAULT (N'Pending');
IF COL_LENGTH(N'migration.Runs', N'StatusReason') IS NULL ALTER TABLE migration.Runs ADD StatusReason nvarchar(2048) NULL;
IF COL_LENGTH(N'migration.Runs', N'EnvironmentName') IS NULL ALTER TABLE migration.Runs ADD EnvironmentName nvarchar(128) NULL;
IF COL_LENGTH(N'migration.Runs', N'IsDryRun') IS NULL ALTER TABLE migration.Runs ADD IsDryRun bit NOT NULL CONSTRAINT DF_Runs_IsDryRun_Compat DEFAULT (0);
IF COL_LENGTH(N'migration.Runs', N'CoordinatorOwner') IS NULL ALTER TABLE migration.Runs ADD CoordinatorOwner nvarchar(256) NULL;
IF COL_LENGTH(N'migration.Runs', N'CoordinationLeaseExpiresUtc') IS NULL ALTER TABLE migration.Runs ADD CoordinationLeaseExpiresUtc datetimeoffset(7) NULL;
IF COL_LENGTH(N'migration.Runs', N'RequestedAtUtc') IS NULL ALTER TABLE migration.Runs ADD RequestedAtUtc datetimeoffset(7) NULL;
IF COL_LENGTH(N'migration.Runs', N'StartedAtUtc') IS NULL ALTER TABLE migration.Runs ADD StartedAtUtc datetimeoffset(7) NULL;
IF COL_LENGTH(N'migration.Runs', N'CompletedAtUtc') IS NULL ALTER TABLE migration.Runs ADD CompletedAtUtc datetimeoffset(7) NULL;
IF COL_LENGTH(N'migration.Runs', N'RequestedCancellationUtc') IS NULL ALTER TABLE migration.Runs ADD RequestedCancellationUtc datetimeoffset(7) NULL;
IF COL_LENGTH(N'migration.Runs', N'CancellationReason') IS NULL ALTER TABLE migration.Runs ADD CancellationReason nvarchar(2048) NULL;
IF COL_LENGTH(N'migration.Runs', N'CompletionEvaluatedUtc') IS NULL ALTER TABLE migration.Runs ADD CompletionEvaluatedUtc datetimeoffset(7) NULL;
IF COL_LENGTH(N'migration.Runs', N'CreatedAtUtc') IS NULL ALTER TABLE migration.Runs ADD CreatedAtUtc datetimeoffset(7) NOT NULL CONSTRAINT DF_Runs_CreatedAtUtc_Compat DEFAULT (sysdatetimeoffset());
IF COL_LENGTH(N'migration.Runs', N'UpdatedAtUtc') IS NULL ALTER TABLE migration.Runs ADD UpdatedAtUtc datetimeoffset(7) NULL;
GO

IF OBJECT_ID(N'migration.MigrationRuns', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'migration.MigrationRuns', N'RunName') IS NULL ALTER TABLE migration.MigrationRuns ADD RunName nvarchar(512) NULL;
    IF COL_LENGTH(N'migration.MigrationRuns', N'EnvironmentName') IS NULL ALTER TABLE migration.MigrationRuns ADD EnvironmentName nvarchar(256) NULL;
    IF COL_LENGTH(N'migration.MigrationRuns', N'IsDryRun') IS NULL ALTER TABLE migration.MigrationRuns ADD IsDryRun bit NOT NULL CONSTRAINT DF_MigrationRuns_IsDryRun_Compat DEFAULT (0);
    IF COL_LENGTH(N'migration.MigrationRuns', N'RequestedAtUtc') IS NULL ALTER TABLE migration.MigrationRuns ADD RequestedAtUtc datetimeoffset(7) NULL;
    IF COL_LENGTH(N'migration.MigrationRuns', N'StartedAtUtc') IS NULL ALTER TABLE migration.MigrationRuns ADD StartedAtUtc datetimeoffset(7) NULL;
    IF COL_LENGTH(N'migration.MigrationRuns', N'CompletedAtUtc') IS NULL ALTER TABLE migration.MigrationRuns ADD CompletedAtUtc datetimeoffset(7) NULL;
    IF COL_LENGTH(N'migration.MigrationRuns', N'UpdatedAtUtc') IS NULL ALTER TABLE migration.MigrationRuns ADD UpdatedAtUtc datetimeoffset(7) NULL;
    IF COL_LENGTH(N'migration.MigrationRuns', N'CompletionEvaluatedUtc') IS NULL ALTER TABLE migration.MigrationRuns ADD CompletionEvaluatedUtc datetimeoffset(7) NULL;
END
GO

IF OBJECT_ID(N'migration.MigrationRuns', N'U') IS NOT NULL
BEGIN
    INSERT INTO migration.Runs
    (
        RunId,
        RunKey,
        RunName,
        SourceSystem,
        TargetSystem,
        Status,
        EnvironmentName,
        IsDryRun,
        RequestedAtUtc,
        StartedAtUtc,
        CompletedAtUtc,
        CompletionEvaluatedUtc,
        CreatedAtUtc,
        UpdatedAtUtc
    )
    SELECT
        mr.RunId,
        COALESCE(NULLIF(mr.RunName, N''), CONVERT(nvarchar(128), mr.RunId)),
        mr.RunName,
        mr.SourceSystem,
        mr.TargetSystem,
        mr.Status,
        mr.EnvironmentName,
        mr.IsDryRun,
        mr.RequestedAtUtc,
        mr.StartedAtUtc,
        mr.CompletedAtUtc,
        mr.CompletionEvaluatedUtc,
        COALESCE(mr.CreatedAt, SYSDATETIMEOFFSET()),
        mr.UpdatedAtUtc
    FROM migration.MigrationRuns mr
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM migration.Runs r
        WHERE r.RunId = mr.RunId
    );
END
GO

IF OBJECT_ID(N'migration.ManifestRows', N'U') IS NULL
BEGIN
    CREATE TABLE migration.ManifestRows
    (
        ManifestRowId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ManifestRows PRIMARY KEY,
        RunId uniqueidentifier NOT NULL,
        ManifestStatus nvarchar(128) NOT NULL CONSTRAINT DF_ManifestRows_ManifestStatus DEFAULT (N'Pending'),
        Operation nvarchar(128) NULL,
        PayloadJson nvarchar(max) NULL,
        SourcePath nvarchar(4000) NULL,
        SourceExternalId nvarchar(1024) NULL,
        SourceRowNumber bigint NULL,
        ContentHash nvarchar(256) NULL,
        ValidationJson nvarchar(max) NULL,
        CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_ManifestRows_CreatedAtUtc DEFAULT (sysutcdatetime()),
        UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_ManifestRows_UpdatedAtUtc DEFAULT (sysutcdatetime())
    );
END
GO

IF COL_LENGTH(N'migration.ManifestRows', N'ManifestStatus') IS NULL ALTER TABLE migration.ManifestRows ADD ManifestStatus nvarchar(128) NOT NULL CONSTRAINT DF_ManifestRows_ManifestStatus_Compat DEFAULT (N'Pending');
IF COL_LENGTH(N'migration.ManifestRows', N'Operation') IS NULL ALTER TABLE migration.ManifestRows ADD Operation nvarchar(128) NULL;
IF COL_LENGTH(N'migration.ManifestRows', N'PayloadJson') IS NULL ALTER TABLE migration.ManifestRows ADD PayloadJson nvarchar(max) NULL;
IF COL_LENGTH(N'migration.ManifestRows', N'SourcePath') IS NULL ALTER TABLE migration.ManifestRows ADD SourcePath nvarchar(4000) NULL;
IF COL_LENGTH(N'migration.ManifestRows', N'SourceExternalId') IS NULL ALTER TABLE migration.ManifestRows ADD SourceExternalId nvarchar(1024) NULL;
IF COL_LENGTH(N'migration.ManifestRows', N'SourceRowNumber') IS NULL ALTER TABLE migration.ManifestRows ADD SourceRowNumber bigint NULL;
IF COL_LENGTH(N'migration.ManifestRows', N'ContentHash') IS NULL ALTER TABLE migration.ManifestRows ADD ContentHash nvarchar(256) NULL;
IF COL_LENGTH(N'migration.ManifestRows', N'ValidationJson') IS NULL ALTER TABLE migration.ManifestRows ADD ValidationJson nvarchar(max) NULL;
IF COL_LENGTH(N'migration.ManifestRows', N'CreatedAtUtc') IS NULL ALTER TABLE migration.ManifestRows ADD CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_ManifestRows_CreatedAtUtc_Compat DEFAULT (sysutcdatetime());
IF COL_LENGTH(N'migration.ManifestRows', N'UpdatedAtUtc') IS NULL ALTER TABLE migration.ManifestRows ADD UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_ManifestRows_UpdatedAtUtc_Compat DEFAULT (sysutcdatetime());
GO

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    CREATE TABLE migration.WorkItems
    (
        WorkItemId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkItems PRIMARY KEY,
        RunId uniqueidentifier NOT NULL,
        ManifestRowId bigint NULL,
        WorkItemType nvarchar(256) NOT NULL,
        Status nvarchar(128) NOT NULL CONSTRAINT DF_WorkItems_Status DEFAULT (N'Queued'),
        PartitionKey nvarchar(256) NULL,
        Priority int NOT NULL CONSTRAINT DF_WorkItems_Priority DEFAULT (100),
        AttemptCount int NOT NULL CONSTRAINT DF_WorkItems_AttemptCount DEFAULT (0),
        MaxAttempts int NOT NULL CONSTRAINT DF_WorkItems_MaxAttempts DEFAULT (5),
        LeaseOwner nvarchar(512) NULL,
        LeaseExpiresUtc datetime2(3) NULL,
        NotBeforeUtc datetime2(3) NULL,
        PayloadJson nvarchar(max) NULL,
        ResultJson nvarchar(max) NULL,
        LastErrorCode nvarchar(256) NULL,
        LastErrorMessage nvarchar(4000) NULL,
        CreatedUtc datetime2(3) NOT NULL CONSTRAINT DF_WorkItems_CreatedUtc DEFAULT (sysutcdatetime()),
        UpdatedUtc datetime2(3) NOT NULL CONSTRAINT DF_WorkItems_UpdatedUtc DEFAULT (sysutcdatetime())
    );
END
GO

IF COL_LENGTH(N'migration.WorkItems', N'ManifestRowId') IS NULL ALTER TABLE migration.WorkItems ADD ManifestRowId bigint NULL;
IF COL_LENGTH(N'migration.WorkItems', N'WorkItemType') IS NULL ALTER TABLE migration.WorkItems ADD WorkItemType nvarchar(256) NULL;
IF COL_LENGTH(N'migration.WorkItems', N'PartitionKey') IS NULL ALTER TABLE migration.WorkItems ADD PartitionKey nvarchar(256) NULL;
IF COL_LENGTH(N'migration.WorkItems', N'Priority') IS NULL ALTER TABLE migration.WorkItems ADD Priority int NOT NULL CONSTRAINT DF_WorkItems_Priority_Compat DEFAULT (100);
IF COL_LENGTH(N'migration.WorkItems', N'MaxAttempts') IS NULL ALTER TABLE migration.WorkItems ADD MaxAttempts int NOT NULL CONSTRAINT DF_WorkItems_MaxAttempts_Compat DEFAULT (5);
IF COL_LENGTH(N'migration.WorkItems', N'LeaseOwner') IS NULL ALTER TABLE migration.WorkItems ADD LeaseOwner nvarchar(512) NULL;
IF COL_LENGTH(N'migration.WorkItems', N'LeaseExpiresUtc') IS NULL ALTER TABLE migration.WorkItems ADD LeaseExpiresUtc datetime2(3) NULL;
IF COL_LENGTH(N'migration.WorkItems', N'NotBeforeUtc') IS NULL ALTER TABLE migration.WorkItems ADD NotBeforeUtc datetime2(3) NULL;
IF COL_LENGTH(N'migration.WorkItems', N'ResultJson') IS NULL ALTER TABLE migration.WorkItems ADD ResultJson nvarchar(max) NULL;
IF COL_LENGTH(N'migration.WorkItems', N'LastErrorCode') IS NULL ALTER TABLE migration.WorkItems ADD LastErrorCode nvarchar(256) NULL;
IF COL_LENGTH(N'migration.WorkItems', N'LastErrorMessage') IS NULL ALTER TABLE migration.WorkItems ADD LastErrorMessage nvarchar(4000) NULL;
IF COL_LENGTH(N'migration.WorkItems', N'CreatedUtc') IS NULL ALTER TABLE migration.WorkItems ADD CreatedUtc datetime2(3) NULL;
IF COL_LENGTH(N'migration.WorkItems', N'UpdatedUtc') IS NULL ALTER TABLE migration.WorkItems ADD UpdatedUtc datetime2(3) NULL;
GO

UPDATE migration.WorkItems
SET
    WorkItemType = COALESCE(WorkItemType, WorkType, N'AssetMigration'),
    PartitionKey = COALESCE(PartitionKey, N'default'),
    Priority = COALESCE(Priority, 100),
    AttemptCount = COALESCE(AttemptCount, 0),
    MaxAttempts = COALESCE(MaxAttempts, 5),
    LeaseExpiresUtc = COALESCE(LeaseExpiresUtc, LeaseExpiresAtUtc),
    NotBeforeUtc = COALESCE(NotBeforeUtc, AvailableAtUtc, SYSUTCDATETIME()),
    PayloadJson = COALESCE(PayloadJson, N'{}'),
    ResultJson = COALESCE(ResultJson, N'{}'),
    CreatedUtc = COALESCE(CreatedUtc, CreatedAtUtc, SYSUTCDATETIME()),
    UpdatedUtc = COALESCE(UpdatedUtc, UpdatedAtUtc, SYSUTCDATETIME())
WHERE
    WorkItemType IS NULL
    OR PartitionKey IS NULL
    OR Priority IS NULL
    OR AttemptCount IS NULL
    OR MaxAttempts IS NULL
    OR NotBeforeUtc IS NULL
    OR PayloadJson IS NULL
    OR ResultJson IS NULL
    OR CreatedUtc IS NULL
    OR UpdatedUtc IS NULL;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'migration.WorkItems')
      AND name = N'IX_WorkItems_Run_Status_Partition_NotBefore'
)
BEGIN
    CREATE INDEX IX_WorkItems_Run_Status_Partition_NotBefore
    ON migration.WorkItems (RunId, Status, PartitionKey, NotBeforeUtc, LeaseExpiresUtc, Priority, CreatedUtc);
END
GO

SELECT 'P7.8B SQL operational runtime bootstrap compatibility applied.' AS Result;
GO
