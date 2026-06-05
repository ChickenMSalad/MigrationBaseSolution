/*
P7.1 SQL operational queue procedures.

These procedures provide the durable queue behaviors needed by P7.2/P7.3:
  - batch claim with lease
  - heartbeat
  - complete
  - fail/retry/dead-letter
  - recover expired leases
*/

SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE migration.usp_ClaimWorkItems
    @WorkerId NVARCHAR(256),
    @BatchSize INT,
    @LeaseSeconds INT = 300,
    @RunId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @BatchSize IS NULL OR @BatchSize <= 0
    BEGIN
        THROW 51000, 'BatchSize must be greater than zero.', 1;
    END;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @LeaseExpiresAtUtc DATETIME2(3) = DATEADD(SECOND, @LeaseSeconds, @NowUtc);

    ;WITH Candidates AS
    (
        SELECT TOP (@BatchSize) wi.WorkItemId
        FROM migration.WorkItems wi WITH (READPAST, UPDLOCK, ROWLOCK, INDEX(IX_WorkItems_ClaimQueue))
        WHERE wi.Status IN (N'Queued', N'RetryScheduled')
          AND wi.AvailableAtUtc <= @NowUtc
          AND (@RunId IS NULL OR wi.RunId = @RunId)
        ORDER BY wi.Priority ASC, wi.AvailableAtUtc ASC, wi.WorkItemId ASC
    )
    UPDATE wi
        SET Status = N'Running',
            ClaimedBy = @WorkerId,
            ClaimedAtUtc = @NowUtc,
            LeaseExpiresAtUtc = @LeaseExpiresAtUtc,
            StartedAtUtc = COALESCE(wi.StartedAtUtc, @NowUtc),
            AttemptCount = wi.AttemptCount + 1,
            UpdatedAtUtc = @NowUtc
    OUTPUT inserted.WorkItemId,
           inserted.RunId,
           inserted.ManifestRowId,
           inserted.WorkType,
           inserted.Status,
           inserted.AttemptCount,
           inserted.MaxAttempts,
           inserted.PayloadJson,
           inserted.LeaseExpiresAtUtc
    FROM migration.WorkItems wi
    INNER JOIN Candidates c ON wi.WorkItemId = c.WorkItemId;
END
GO

CREATE OR ALTER PROCEDURE migration.usp_CompleteWorkItem
    @WorkItemId BIGINT,
    @WorkerId NVARCHAR(256),
    @ResultJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    UPDATE migration.WorkItems
        SET Status = N'Completed',
            CompletedAtUtc = @NowUtc,
            LeaseExpiresAtUtc = NULL,
            ResultJson = @ResultJson,
            UpdatedAtUtc = @NowUtc
    WHERE WorkItemId = @WorkItemId
      AND Status = N'Running'
      AND ClaimedBy = @WorkerId;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51001, 'Work item was not completed because it is not currently leased by the supplied worker.', 1;
    END;
END
GO

CREATE OR ALTER PROCEDURE migration.usp_FailWorkItem
    @WorkItemId BIGINT,
    @WorkerId NVARCHAR(256),
    @ErrorCode NVARCHAR(128) = NULL,
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @ExceptionType NVARCHAR(512) = NULL,
    @IsRetryable BIT = 1,
    @RetryDelaySeconds INT = 300,
    @FailureJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @RunId UNIQUEIDENTIFIER;
    DECLARE @AttemptNumber INT;
    DECLARE @MaxAttempts INT;

    SELECT
        @RunId = RunId,
        @AttemptNumber = AttemptCount,
        @MaxAttempts = MaxAttempts
    FROM migration.WorkItems WITH (UPDLOCK, ROWLOCK)
    WHERE WorkItemId = @WorkItemId
      AND Status = N'Running'
      AND ClaimedBy = @WorkerId;

    IF @RunId IS NULL
    BEGIN
        THROW 51002, 'Work item was not failed because it is not currently leased by the supplied worker.', 1;
    END;

    INSERT INTO migration.WorkItemFailures
    (
        WorkItemId,
        RunId,
        AttemptNumber,
        ErrorCode,
        ErrorMessage,
        ExceptionType,
        IsRetryable,
        FailureJson,
        CreatedAtUtc
    )
    VALUES
    (
        @WorkItemId,
        @RunId,
        @AttemptNumber,
        @ErrorCode,
        @ErrorMessage,
        @ExceptionType,
        @IsRetryable,
        @FailureJson,
        @NowUtc
    );

    UPDATE migration.WorkItems
        SET Status = CASE
                        WHEN @IsRetryable = 1 AND AttemptCount < MaxAttempts THEN N'RetryScheduled'
                        ELSE N'DeadLettered'
                     END,
            AvailableAtUtc = CASE
                                WHEN @IsRetryable = 1 AND AttemptCount < MaxAttempts THEN DATEADD(SECOND, @RetryDelaySeconds, @NowUtc)
                                ELSE AvailableAtUtc
                             END,
            LeaseExpiresAtUtc = NULL,
            LastErrorCode = @ErrorCode,
            LastErrorMessage = LEFT(@ErrorMessage, 2048),
            UpdatedAtUtc = @NowUtc
    WHERE WorkItemId = @WorkItemId;
END
GO

CREATE OR ALTER PROCEDURE migration.usp_RecoverExpiredLeases
    @MaxItems INT = 1000
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    ;WITH Expired AS
    (
        SELECT TOP (@MaxItems) WorkItemId
        FROM migration.WorkItems WITH (READPAST, UPDLOCK, ROWLOCK, INDEX(IX_WorkItems_LeaseRecovery))
        WHERE Status = N'Running'
          AND LeaseExpiresAtUtc IS NOT NULL
          AND LeaseExpiresAtUtc < @NowUtc
        ORDER BY LeaseExpiresAtUtc ASC, WorkItemId ASC
    )
    UPDATE wi
        SET Status = CASE WHEN wi.AttemptCount < wi.MaxAttempts THEN N'RetryScheduled' ELSE N'DeadLettered' END,
            AvailableAtUtc = @NowUtc,
            ClaimedBy = NULL,
            ClaimedAtUtc = NULL,
            LeaseExpiresAtUtc = NULL,
            LastErrorCode = COALESCE(wi.LastErrorCode, N'LeaseExpired'),
            LastErrorMessage = COALESCE(wi.LastErrorMessage, N'Work item lease expired before completion.'),
            UpdatedAtUtc = @NowUtc
    OUTPUT inserted.WorkItemId,
           inserted.RunId,
           inserted.Status,
           inserted.AttemptCount,
           inserted.MaxAttempts
    FROM migration.WorkItems wi
    INNER JOIN Expired e ON wi.WorkItemId = e.WorkItemId;
END
GO

CREATE OR ALTER PROCEDURE migration.usp_RecordWorkerHeartbeat
    @WorkerId NVARCHAR(256),
    @HostName NVARCHAR(256) = NULL,
    @ProcessId INT = NULL,
    @RuntimeVersion NVARCHAR(128) = NULL,
    @CurrentRunId UNIQUEIDENTIFIER = NULL,
    @Status NVARCHAR(64) = N'Healthy',
    @DetailsJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    MERGE migration.WorkerHeartbeats AS target
    USING (SELECT @WorkerId AS WorkerId) AS source
        ON target.WorkerId = source.WorkerId
    WHEN MATCHED THEN
        UPDATE SET HostName = @HostName,
                   ProcessId = @ProcessId,
                   RuntimeVersion = @RuntimeVersion,
                   LastHeartbeatUtc = SYSUTCDATETIME(),
                   CurrentRunId = @CurrentRunId,
                   Status = @Status,
                   DetailsJson = @DetailsJson
    WHEN NOT MATCHED THEN
        INSERT (WorkerId, HostName, ProcessId, RuntimeVersion, LastHeartbeatUtc, CurrentRunId, Status, DetailsJson)
        VALUES (@WorkerId, @HostName, @ProcessId, @RuntimeVersion, SYSUTCDATETIME(), @CurrentRunId, @Status, @DetailsJson);
END
GO
