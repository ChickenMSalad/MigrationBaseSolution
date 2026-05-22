if not exists (
    select 1
    from sys.indexes
    where name = 'IX_OperationalWorkItems_LeaseExpiration'
      and object_id = object_id('[migration].[OperationalWorkItems]'))
begin
    create index IX_OperationalWorkItems_LeaseExpiration
        on [migration].[OperationalWorkItems] (Status, LeaseExpiresUtc, RunId)
        include (WorkItemId, LeaseOwner, AttemptCount, MaxAttempts, CreatedUtc);
end;

go

if not exists (
    select 1
    from sys.indexes
    where name = 'IX_OperationalWorkItems_LeaseOwner'
      and object_id = object_id('[migration].[OperationalWorkItems]'))
begin
    create index IX_OperationalWorkItems_LeaseOwner
        on [migration].[OperationalWorkItems] (LeaseOwner, Status, LeaseExpiresUtc)
        include (WorkItemId, RunId, UpdatedUtc);
end;
