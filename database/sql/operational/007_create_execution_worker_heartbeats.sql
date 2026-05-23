IF OBJECT_ID(N'dbo.MigrationExecutionWorkerHeartbeats', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationExecutionWorkerHeartbeats
    (
        WorkerId NVARCHAR(256) NOT NULL CONSTRAINT PK_MigrationExecutionWorkerHeartbeats PRIMARY KEY,
        ExecutionSessionId UNIQUEIDENTIFIER NULL,
        Status NVARCHAR(64) NOT NULL,
        LastHeartbeatUtc DATETIMEOFFSET NOT NULL,
        ActiveLeaseCount INT NOT NULL CONSTRAINT DF_MigrationExecutionWorkerHeartbeats_ActiveLeaseCount DEFAULT 0,
        Message NVARCHAR(1024) NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationExecutionWorkerHeartbeats_CreatedUtc DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_MigrationExecutionWorkerHeartbeats_LastHeartbeatUtc
        ON dbo.MigrationExecutionWorkerHeartbeats (LastHeartbeatUtc DESC);

    CREATE INDEX IX_MigrationExecutionWorkerHeartbeats_ExecutionSessionId
        ON dbo.MigrationExecutionWorkerHeartbeats (ExecutionSessionId, LastHeartbeatUtc DESC);
END
