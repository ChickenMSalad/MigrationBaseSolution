set ansi_nulls on;
set quoted_identifier on;
go

if object_id(N'dbo.MigrationRuns', N'U') is not null
begin
    if col_length(N'dbo.MigrationRuns', N'StatusReason') is null
        alter table dbo.MigrationRuns add StatusReason nvarchar(max) null;

    if col_length(N'dbo.MigrationRuns', N'CoordinatorOwner') is null
        alter table dbo.MigrationRuns add CoordinatorOwner nvarchar(256) null;

    if col_length(N'dbo.MigrationRuns', N'CoordinationLeaseExpiresUtc') is null
        alter table dbo.MigrationRuns add CoordinationLeaseExpiresUtc datetimeoffset null;

    if col_length(N'dbo.MigrationRuns', N'RequestedCancellationUtc') is null
        alter table dbo.MigrationRuns add RequestedCancellationUtc datetimeoffset null;

    if col_length(N'dbo.MigrationRuns', N'CancellationReason') is null
        alter table dbo.MigrationRuns add CancellationReason nvarchar(max) null;

    if col_length(N'dbo.MigrationRuns', N'FanOutStartedUtc') is null
        alter table dbo.MigrationRuns add FanOutStartedUtc datetimeoffset null;

    if col_length(N'dbo.MigrationRuns', N'FanOutCompletedUtc') is null
        alter table dbo.MigrationRuns add FanOutCompletedUtc datetimeoffset null;

    if col_length(N'dbo.MigrationRuns', N'CompletionEvaluatedUtc') is null
        alter table dbo.MigrationRuns add CompletionEvaluatedUtc datetimeoffset null;
end;
go

if object_id(N'dbo.MigrationRuns', N'U') is not null
   and not exists (select 1 from sys.indexes where name = N'IX_MigrationRuns_Status_CoordinationLease' and object_id = object_id(N'dbo.MigrationRuns'))
begin
    create index IX_MigrationRuns_Status_CoordinationLease
        on dbo.MigrationRuns (Status, CoordinationLeaseExpiresUtc)
        include (ProjectId, StartedAtUtc, UpdatedAtUtc);
end;
go

if object_id(N'dbo.MigrationManifestRows', N'U') is not null
   and not exists (select 1 from sys.indexes where name = N'IX_MigrationManifestRows_Run_Status_Row' and object_id = object_id(N'dbo.MigrationManifestRows'))
begin
    create index IX_MigrationManifestRows_Run_Status_Row
        on dbo.MigrationManifestRows (RunId, Status, RowNumber)
        include (ManifestRowId);
end;
go
