IF OBJECT_ID(N'dbo.MigrationExecutionReplayApprovals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationExecutionReplayApprovals
    (
        ReplayApprovalId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MigrationExecutionReplayApprovals PRIMARY KEY,
        SourceExecutionSessionId UNIQUEIDENTIFIER NOT NULL,
        Scope NVARCHAR(64) NOT NULL,
        ApprovedBy NVARCHAR(256) NOT NULL,
        ApprovalNote NVARCHAR(2048) NOT NULL,
        Status NVARCHAR(64) NOT NULL,
        ExpiresUtc DATETIMEOFFSET NOT NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationExecutionReplayApprovals_CreatedUtc DEFAULT SYSUTCDATETIME(),
        ConsumedUtc DATETIMEOFFSET NULL,
        ReplayExecutionSessionId UNIQUEIDENTIFIER NULL
    );

    CREATE INDEX IX_MigrationExecutionReplayApprovals_Source_Status_Expires
        ON dbo.MigrationExecutionReplayApprovals (SourceExecutionSessionId, Status, ExpiresUtc DESC);
END
