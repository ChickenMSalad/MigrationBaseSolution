IF OBJECT_ID(N'dbo.MigrationExecutionSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationExecutionSessions
    (
        ExecutionSessionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MigrationExecutionSessions PRIMARY KEY,
        MigrationRunId UNIQUEIDENTIFIER NULL,
        Name NVARCHAR(256) NOT NULL,
        SourceConnector NVARCHAR(128) NULL,
        TargetConnector NVARCHAR(128) NULL,
        Status NVARCHAR(64) NOT NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationExecutionSessions_CreatedUtc DEFAULT SYSUTCDATETIME(),
        StartedUtc DATETIMEOFFSET NULL,
        CompletedUtc DATETIMEOFFSET NULL,
        Notes NVARCHAR(2048) NULL
    );

    CREATE INDEX IX_MigrationExecutionSessions_CreatedUtc
        ON dbo.MigrationExecutionSessions (CreatedUtc DESC);

    CREATE INDEX IX_MigrationExecutionSessions_Status_CreatedUtc
        ON dbo.MigrationExecutionSessions (Status, CreatedUtc DESC);
END

IF COL_LENGTH(N'dbo.MigrationOperationalEvents', N'ExecutionSessionId') IS NULL
BEGIN
    ALTER TABLE dbo.MigrationOperationalEvents
        ADD ExecutionSessionId UNIQUEIDENTIFIER NULL;
END

IF COL_LENGTH(N'dbo.MigrationOperationalEvents', N'MigrationRunId') IS NULL
BEGIN
    ALTER TABLE dbo.MigrationOperationalEvents
        ADD MigrationRunId UNIQUEIDENTIFIER NULL;
END

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_MigrationOperationalEvents_ExecutionSessionId_CreatedUtc'
      AND object_id = OBJECT_ID(N'dbo.MigrationOperationalEvents')
)
BEGIN
    CREATE INDEX IX_MigrationOperationalEvents_ExecutionSessionId_CreatedUtc
        ON dbo.MigrationOperationalEvents (ExecutionSessionId, CreatedUtc DESC);
END
