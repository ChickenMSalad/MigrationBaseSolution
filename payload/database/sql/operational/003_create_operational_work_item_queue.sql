if schema_id(N'dbo') is null
begin
    execute(N'create schema dbo');
end;

go

if object_id(N'dbo.MigrationWorkItems', N'U') is null
begin
    create table dbo.MigrationWorkItems
    (
        WorkItemId uniqueidentifier not null constraint PK_MigrationWorkItems primary key,
        RunId uniqueidentifier not null,
        ManifestRowId uniqueidentifier null,
        WorkItemType nvarchar(128) not null,
        Status nvarchar(64) not null,
        PartitionKey nvarchar(256) null,
        Priority int not null constraint DF_MigrationWorkItems_Priority default 0,
        AttemptCount int not null constraint DF_MigrationWorkItems_AttemptCount default 0,
        MaxAttempts int not null constraint DF_MigrationWorkItems_MaxAttempts default 5,
        LeaseOwner nvarchar(256) null,
        LeaseExpiresUtc datetimeoffset null,
        NotBeforeUtc datetimeoffset null,
        PayloadJson nvarchar(max) null,
        ResultJson nvarchar(max) null,
        LastErrorCode nvarchar(256) null,
        LastErrorMessage nvarchar(max) null,
        CreatedUtc datetimeoffset not null,
        UpdatedUtc datetimeoffset not null
    );
end;

go

if not exists (select 1 from sys.indexes where name = N'IX_MigrationWorkItems_Run_Status_Claim' and object_id = object_id(N'dbo.MigrationWorkItems'))
begin
    create index IX_MigrationWorkItems_Run_Status_Claim
        on dbo.MigrationWorkItems (RunId, Status, PartitionKey, NotBeforeUtc, LeaseExpiresUtc, Priority desc, CreatedUtc asc)
        include (AttemptCount, MaxAttempts);
end;

go

if not exists (select 1 from sys.indexes where name = N'IX_MigrationWorkItems_Run_Summary' and object_id = object_id(N'dbo.MigrationWorkItems'))
begin
    create index IX_MigrationWorkItems_Run_Summary
        on dbo.MigrationWorkItems (RunId, Status, UpdatedUtc)
        include (CreatedUtc);
end;

go
