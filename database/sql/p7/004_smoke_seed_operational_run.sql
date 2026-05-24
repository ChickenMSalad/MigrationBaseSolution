/*
P7.7D smoke seed for SQL operational lifecycle validation.
Review table names before running if your P7 schema differs.
This script assumes the P7 operational schema uses [migration] and the table names created by prior P7 scripts.
*/

set nocount on;

begin transaction;

begin try
    declare @RunId uniqueidentifier = '77777777-7777-4777-8777-77777777777D';
    declare @ProjectId uniqueidentifier = '77777777-7777-4777-8777-777777777001';
    declare @NowUtc datetimeoffset = sysutcdatetime();

    if object_id(N'[migration].[OperationalRuns]', N'U') is null
    begin
        throw 51000, 'Required table [migration].[OperationalRuns] was not found. Apply P7 SQL scripts 001-003 first, or adjust this smoke script to match your schema.', 1;
    end;

    if object_id(N'[migration].[OperationalManifestRows]', N'U') is null
    begin
        throw 51000, 'Required table [migration].[OperationalManifestRows] was not found. Apply P7 SQL scripts 001-003 first, or adjust this smoke script to match your schema.', 1;
    end;

    if not exists (select 1 from [migration].[OperationalRuns] where RunId = @RunId)
    begin
        insert into [migration].[OperationalRuns] (
            RunId,
            ProjectId,
            RunKey,
            Status,
            StatusReason,
            CoordinatorOwner,
            CoordinationLeaseExpiresUtc,
            StartedAtUtc,
            CompletedAtUtc,
            RequestedCancellationUtc,
            CancellationReason,
            CreatedAtUtc,
            UpdatedAtUtc)
        values (
            @RunId,
            @ProjectId,
            N'p7-smoke-sql-operational-lifecycle',
            N'Ready',
            N'P7.7D smoke seed',
            null,
            null,
            null,
            null,
            null,
            null,
            @NowUtc,
            @NowUtc);
    end;

    if not exists (select 1 from [migration].[OperationalManifestRows] where RunId = @RunId)
    begin
        insert into [migration].[OperationalManifestRows] (
            ManifestRowId,
            RunId,
            RowNumber,
            Status,
            SourceIdentifier,
            TargetIdentifier,
            PayloadJson,
            CreatedAtUtc,
            UpdatedAtUtc)
        values
            (newid(), @RunId, 1, N'Ready', N'p7-smoke-source-001', null, N'{"smoke":true,"row":1}', @NowUtc, @NowUtc),
            (newid(), @RunId, 2, N'Ready', N'p7-smoke-source-002', null, N'{"smoke":true,"row":2}', @NowUtc, @NowUtc),
            (newid(), @RunId, 3, N'Ready', N'p7-smoke-source-003', null, N'{"smoke":true,"row":3}', @NowUtc, @NowUtc);
    end;

    select @RunId as SmokeRunId;

    commit transaction;
end try
begin catch
    if @@trancount > 0 rollback transaction;
    throw;
end catch;
