IF OBJECT_ID(N'dbo.MigrationExecutionWorkItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationExecutionWorkItems
    (
        ExecutionWorkItemId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MigrationExecutionWorkItems PRIMARY KEY,
        ExecutionSessionId UNIQUEIDENTIFIER NOT NULL,
        MigrationRunId UNIQUEIDENTIFIER NULL,
        ExecutionPlanStepId UNIQUEIDENTIFIER NULL,
        WorkItemType NVARCHAR(128) NOT NULL,
        WorkItemName NVARCHAR(256) NOT NULL,
        Status NVARCHAR(64) NOT NULL,
        Priority INT NOT NULL CONSTRAINT DF_MigrationExecutionWorkItems_Priority DEFAULT 100,
        RetryCount INT NOT NULL CONSTRAINT DF_MigrationExecutionWorkItems_RetryCount DEFAULT 0,
        MaxRetries INT NOT NULL CONSTRAINT DF_MigrationExecutionWorkItems_MaxRetries DEFAULT 3,
        WorkerId NVARCHAR(256) NULL,
        LeaseId UNIQUEIDENTIFIER NULL,
        LeaseExpiresUtc DATETIMEOFFSET NULL,
        PayloadJson NVARCHAR(MAX) NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationExecutionWorkItems_CreatedUtc DEFAULT SYSUTCDATETIME(),
        StartedUtc DATETIMEOFFSET NULL,
        CompletedUtc DATETIMEOFFSET NULL,
        ErrorMessage NVARCHAR(2048) NULL
    );

    CREATE INDEX IX_MigrationExecutionWorkItems_Session_Status_Priority
        ON dbo.MigrationExecutionWorkItems (ExecutionSessionId, Status, Priority ASC, CreatedUtc ASC);

    CREATE INDEX IX_MigrationExecutionWorkItems_Status_LeaseExpiresUtc
        ON dbo.MigrationExecutionWorkItems (Status, LeaseExpiresUtc ASC);

    CREATE INDEX IX_MigrationExecutionWorkItems_LeaseId
        ON dbo.MigrationExecutionWorkItems (LeaseId);
END
