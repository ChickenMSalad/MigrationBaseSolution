IF SCHEMA_ID(N'migration') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [migration]');
END;

IF OBJECT_ID(N'[migration].[DispatcherExecutions]', N'U') IS NULL
BEGIN
    CREATE TABLE [migration].[DispatcherExecutions]
    (
        [ExecutionId] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_DispatcherExecutions] PRIMARY KEY,
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

    CREATE INDEX [IX_DispatcherExecutions_StartedAt]
        ON [migration].[DispatcherExecutions] ([StartedAt] DESC);

    CREATE INDEX [IX_DispatcherExecutions_WorkerId_StartedAt]
        ON [migration].[DispatcherExecutions] ([WorkerId], [StartedAt] DESC);
END;
