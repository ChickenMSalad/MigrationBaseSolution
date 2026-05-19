CREATE TABLE [migration].[DispatcherExecutions]
(
    [ExecutionId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [WorkerId] NVARCHAR(200) NOT NULL,
    [StartedAt] DATETIMEOFFSET NOT NULL,
    [CompletedAt] DATETIMEOFFSET NOT NULL,
    [DurationMilliseconds] BIGINT NOT NULL,
    [RequestedLeaseCount] INT NOT NULL,
    [LeasedCount] INT NOT NULL,
    [CompletedCount] INT NOT NULL,
    [FailedCount] INT NOT NULL,
    [Outcome] NVARCHAR(100) NOT NULL,
    [Message] NVARCHAR(4000) NOT NULL
);
