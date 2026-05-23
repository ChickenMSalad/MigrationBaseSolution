IF OBJECT_ID(N'dbo.MigrationExecutionReplayPolicyEvaluations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationExecutionReplayPolicyEvaluations
    (
        ReplayPolicyEvaluationId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MigrationExecutionReplayPolicyEvaluations PRIMARY KEY,
        SourceExecutionSessionId UNIQUEIDENTIFIER NOT NULL,
        Scope NVARCHAR(64) NOT NULL,
        Decision NVARCHAR(32) NOT NULL,
        PolicyScore INT NOT NULL,
        MetricsJson NVARCHAR(MAX) NOT NULL,
        ViolationsJson NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationExecutionReplayPolicyEvaluations_CreatedUtc DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_MigrationExecutionReplayPolicyEvaluations_Source_Created
        ON dbo.MigrationExecutionReplayPolicyEvaluations (SourceExecutionSessionId, CreatedUtc DESC);
END
