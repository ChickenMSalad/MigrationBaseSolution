IF OBJECT_ID(N'dbo.MigrationExecutionPhaseHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationExecutionPhaseHistory
    (
        ExecutionPhaseHistoryId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MigrationExecutionPhaseHistory PRIMARY KEY,
        ExecutionSessionId UNIQUEIDENTIFIER NOT NULL,
        MigrationRunId UNIQUEIDENTIFIER NULL,
        PreviousPhase NVARCHAR(64) NULL,
        NewPhase NVARCHAR(64) NOT NULL,
        Reason NVARCHAR(1024) NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationExecutionPhaseHistory_CreatedUtc DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_MigrationExecutionPhaseHistory_ExecutionSessionId_CreatedUtc
        ON dbo.MigrationExecutionPhaseHistory (ExecutionSessionId, CreatedUtc DESC);

    CREATE INDEX IX_MigrationExecutionPhaseHistory_NewPhase_CreatedUtc
        ON dbo.MigrationExecutionPhaseHistory (NewPhase, CreatedUtc DESC);
END
