IF OBJECT_ID(N'dbo.MigrationExecutionReplayPolicyOverrides', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationExecutionReplayPolicyOverrides
    (
        ReplayPolicyOverrideId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MigrationExecutionReplayPolicyOverrides PRIMARY KEY,
        SourceExecutionSessionId UNIQUEIDENTIFIER NOT NULL,
        Scope NVARCHAR(64) NOT NULL,
        PolicyDecision NVARCHAR(32) NOT NULL,
        PolicyScore INT NOT NULL,
        OverriddenBy NVARCHAR(256) NOT NULL,
        OverrideReason NVARCHAR(2048) NOT NULL,
        Status NVARCHAR(64) NOT NULL,
        ExpiresUtc DATETIMEOFFSET NOT NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationExecutionReplayPolicyOverrides_CreatedUtc DEFAULT SYSUTCDATETIME(),
        ConsumedUtc DATETIMEOFFSET NULL,
        ReplayExecutionSessionId UNIQUEIDENTIFIER NULL
    );

    CREATE INDEX IX_MigrationExecutionReplayPolicyOverrides_Source_Status_Expires
        ON dbo.MigrationExecutionReplayPolicyOverrides (SourceExecutionSessionId, Status, ExpiresUtc DESC);
END
