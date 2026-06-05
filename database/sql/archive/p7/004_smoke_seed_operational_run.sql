SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @RunId uniqueidentifier = '11111111-1111-1111-1111-111111111111';
DECLARE @WorkItemId uniqueidentifier = '22222222-2222-2222-2222-222222222222';
DECLARE @Now datetimeoffset(7) = SYSDATETIMEOFFSET();

IF NOT EXISTS (SELECT 1 FROM migration.MigrationRuns WHERE RunId = @RunId)
BEGIN
    INSERT INTO migration.MigrationRuns (
        RunId,
        SourceSystem,
        TargetSystem,
        Status,
        CreatedAt,
        RunName,
        EnvironmentName,
        IsDryRun,
        RequestedAtUtc,
        UpdatedAtUtc
    )
    VALUES (
        @RunId,
        N'SmokeSource',
        N'SmokeTarget',
        N'Pending',
        @Now,
        N'p7-sql-operational-smoke',
        N'Development',
        1,
        @Now,
        @Now
    );
END
GO

DECLARE @RunId uniqueidentifier = '11111111-1111-1111-1111-111111111111';
DECLARE @WorkItemId uniqueidentifier = '22222222-2222-2222-2222-222222222222';
DECLARE @Now datetimeoffset(7) = SYSDATETIMEOFFSET();

IF NOT EXISTS (SELECT 1 FROM migration.MigrationWorkItems WHERE WorkItemId = @WorkItemId)
BEGIN
    INSERT INTO migration.MigrationWorkItems (
        WorkItemId,
        RunId,
        Status,
        CreatedAt,
        UpdatedAtUtc
    )
    VALUES (
        @WorkItemId,
        @RunId,
        N'Pending',
        @Now,
        @Now
    );
END
GO