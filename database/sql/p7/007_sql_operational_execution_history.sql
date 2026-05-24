SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF SCHEMA_ID(N'migration') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA migration');
END
GO

IF OBJECT_ID(N'migration.WorkItemExecutionAttempts', N'U') IS NULL
BEGIN
    CREATE TABLE migration.WorkItemExecutionAttempts
    (
        ExecutionAttemptId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkItemExecutionAttempts PRIMARY KEY,
        WorkItemId bigint NOT NULL,
        RunId uniqueidentifier NOT NULL,
        ManifestRowId bigint NULL,
        WorkItemType nvarchar(256) NOT NULL,
        AttemptNumber int NOT NULL,
        WorkerId nvarchar(512) NOT NULL,
        Status nvarchar(128) NOT NULL,
        StartedUtc datetimeoffset(7) NOT NULL CONSTRAINT DF_WorkItemExecutionAttempts_StartedUtc DEFAULT (sysdatetimeoffset()),
        CompletedUtc datetimeoffset(7) NULL,
        DurationMilliseconds bigint NULL,
        ErrorCode nvarchar(256) NULL,
        ErrorMessage nvarchar(4000) NULL,
        IsRetryable bit NULL,
        PayloadJson nvarchar(max) NULL,
        ResultJson nvarchar(max) NULL,
        CreatedUtc datetimeoffset(7) NOT NULL CONSTRAINT DF_WorkItemExecutionAttempts_CreatedUtc DEFAULT (sysdatetimeoffset()),
        UpdatedUtc datetimeoffset(7) NOT NULL CONSTRAINT DF_WorkItemExecutionAttempts_UpdatedUtc DEFAULT (sysdatetimeoffset())
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'migration.WorkItemExecutionAttempts', N'U')
      AND name = N'IX_WorkItemExecutionAttempts_Run_WorkItem_Attempt'
)
BEGIN
    CREATE INDEX IX_WorkItemExecutionAttempts_Run_WorkItem_Attempt
    ON migration.WorkItemExecutionAttempts (RunId, WorkItemId, AttemptNumber);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'migration.WorkItemExecutionAttempts', N'U')
      AND name = N'IX_WorkItemExecutionAttempts_Status_StartedUtc'
)
BEGIN
    CREATE INDEX IX_WorkItemExecutionAttempts_Status_StartedUtc
    ON migration.WorkItemExecutionAttempts (Status, StartedUtc);
END
GO

IF OBJECT_ID(N'migration.vw_WorkItemExecutionAttemptSummary', N'V') IS NOT NULL
BEGIN
    DROP VIEW migration.vw_WorkItemExecutionAttemptSummary;
END
GO

CREATE VIEW migration.vw_WorkItemExecutionAttemptSummary
AS
SELECT
    RunId,
    WorkItemType,
    Status,
    COUNT_BIG(*) AS AttemptCount,
    MIN(StartedUtc) AS FirstStartedUtc,
    MAX(COALESCE(CompletedUtc, UpdatedUtc, StartedUtc)) AS LastUpdatedUtc,
    SUM(CASE WHEN Status = N'Succeeded' THEN 1 ELSE 0 END) AS SucceededCount,
    SUM(CASE WHEN Status = N'Failed' THEN 1 ELSE 0 END) AS FailedCount,
    SUM(CASE WHEN IsRetryable = 1 THEN 1 ELSE 0 END) AS RetryableCount
FROM migration.WorkItemExecutionAttempts
GROUP BY RunId, WorkItemType, Status;
GO

IF OBJECT_ID(N'migration.usp_RecordWorkItemExecutionAttemptStarted', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE migration.usp_RecordWorkItemExecutionAttemptStarted;
END
GO

CREATE PROCEDURE migration.usp_RecordWorkItemExecutionAttemptStarted
    @WorkItemId bigint,
    @RunId uniqueidentifier,
    @ManifestRowId bigint = NULL,
    @WorkItemType nvarchar(256),
    @AttemptNumber int,
    @WorkerId nvarchar(512),
    @PayloadJson nvarchar(max) = NULL,
    @ExecutionAttemptId bigint OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO migration.WorkItemExecutionAttempts
    (
        WorkItemId,
        RunId,
        ManifestRowId,
        WorkItemType,
        AttemptNumber,
        WorkerId,
        Status,
        PayloadJson
    )
    VALUES
    (
        @WorkItemId,
        @RunId,
        @ManifestRowId,
        @WorkItemType,
        @AttemptNumber,
        @WorkerId,
        N'Started',
        @PayloadJson
    );

    SET @ExecutionAttemptId = CONVERT(bigint, SCOPE_IDENTITY());
END
GO

IF OBJECT_ID(N'migration.usp_RecordWorkItemExecutionAttemptCompleted', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE migration.usp_RecordWorkItemExecutionAttemptCompleted;
END
GO

CREATE PROCEDURE migration.usp_RecordWorkItemExecutionAttemptCompleted
    @ExecutionAttemptId bigint,
    @Succeeded bit,
    @ErrorCode nvarchar(256) = NULL,
    @ErrorMessage nvarchar(4000) = NULL,
    @IsRetryable bit = NULL,
    @ResultJson nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now datetimeoffset(7) = SYSDATETIMEOFFSET();

    UPDATE migration.WorkItemExecutionAttempts
    SET Status = CASE WHEN @Succeeded = 1 THEN N'Succeeded' ELSE N'Failed' END,
        CompletedUtc = @Now,
        DurationMilliseconds = DATEDIFF_BIG(millisecond, StartedUtc, @Now),
        ErrorCode = @ErrorCode,
        ErrorMessage = @ErrorMessage,
        IsRetryable = @IsRetryable,
        ResultJson = @ResultJson,
        UpdatedUtc = @Now
    WHERE ExecutionAttemptId = @ExecutionAttemptId;
END
GO

SELECT N'P7.9A SQL operational execution history applied.' AS Result;
GO
