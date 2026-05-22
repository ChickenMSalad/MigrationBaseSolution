IF OBJECT_ID(N'dbo.MigrationExecutionPlanSteps', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationExecutionPlanSteps
    (
        ExecutionPlanStepId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MigrationExecutionPlanSteps PRIMARY KEY,
        ExecutionSessionId UNIQUEIDENTIFIER NOT NULL,
        MigrationRunId UNIQUEIDENTIFIER NULL,
        StepOrder INT NOT NULL,
        StepType NVARCHAR(128) NOT NULL,
        StepName NVARCHAR(256) NOT NULL,
        Status NVARCHAR(64) NOT NULL,
        SourceConnector NVARCHAR(128) NULL,
        TargetConnector NVARCHAR(128) NULL,
        PayloadJson NVARCHAR(MAX) NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationExecutionPlanSteps_CreatedUtc DEFAULT SYSUTCDATETIME(),
        StartedUtc DATETIMEOFFSET NULL,
        CompletedUtc DATETIMEOFFSET NULL,
        ErrorMessage NVARCHAR(2048) NULL
    );

    CREATE INDEX IX_MigrationExecutionPlanSteps_ExecutionSessionId_StepOrder
        ON dbo.MigrationExecutionPlanSteps (ExecutionSessionId, StepOrder ASC);

    CREATE INDEX IX_MigrationExecutionPlanSteps_Status_CreatedUtc
        ON dbo.MigrationExecutionPlanSteps (Status, CreatedUtc DESC);
END
