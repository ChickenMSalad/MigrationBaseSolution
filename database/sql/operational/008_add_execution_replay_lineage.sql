IF COL_LENGTH(N'dbo.MigrationExecutionSessions', N'ReplaySourceExecutionSessionId') IS NULL
BEGIN
    ALTER TABLE dbo.MigrationExecutionSessions
        ADD ReplaySourceExecutionSessionId UNIQUEIDENTIFIER NULL;
END

IF COL_LENGTH(N'dbo.MigrationExecutionSessions', N'ReplayScope') IS NULL
BEGIN
    ALTER TABLE dbo.MigrationExecutionSessions
        ADD ReplayScope NVARCHAR(64) NULL;
END

IF COL_LENGTH(N'dbo.MigrationExecutionSessions', N'ReplayDepth') IS NULL
BEGIN
    ALTER TABLE dbo.MigrationExecutionSessions
        ADD ReplayDepth INT NOT NULL CONSTRAINT DF_MigrationExecutionSessions_ReplayDepth DEFAULT 0;
END

IF COL_LENGTH(N'dbo.MigrationExecutionSessions', N'ReplayApprovalNote') IS NULL
BEGIN
    ALTER TABLE dbo.MigrationExecutionSessions
        ADD ReplayApprovalNote NVARCHAR(2048) NULL;
END

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_MigrationExecutionSessions_ReplaySourceExecutionSessionId'
      AND object_id = OBJECT_ID(N'dbo.MigrationExecutionSessions')
)
BEGIN
    CREATE INDEX IX_MigrationExecutionSessions_ReplaySourceExecutionSessionId
        ON dbo.MigrationExecutionSessions (ReplaySourceExecutionSessionId, CreatedUtc DESC);
END
