IF OBJECT_ID(N'dbo.MigrationExecutionReplayAdmissionDecisions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationExecutionReplayAdmissionDecisions
    (
        ReplayAdmissionDecisionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MigrationExecutionReplayAdmissionDecisions PRIMARY KEY,
        ExecutionSessionId UNIQUEIDENTIFIER NOT NULL,
        Decision NVARCHAR(64) NOT NULL,
        Reason NVARCHAR(2048) NOT NULL,
        ActiveReplayCount INT NOT NULL,
        MaxConcurrentReplays INT NOT NULL,
        WithinAllowedWindow BIT NOT NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationExecutionReplayAdmissionDecisions_CreatedUtc DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_MigrationExecutionReplayAdmissionDecisions_Session_Created
        ON dbo.MigrationExecutionReplayAdmissionDecisions (ExecutionSessionId, CreatedUtc DESC);
END
