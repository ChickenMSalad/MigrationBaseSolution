# P3 SQL Schema Starting Point

## Purpose

This document defines the initial SQL Server model for P3 planning.

It is intentionally not a final migration script yet.

## Core tables

### MigrationProject

Represents a reusable migration setup.

Suggested columns:

```sql
ProjectId uniqueidentifier primary key
WorkspaceId nvarchar(100) not null
ProjectName nvarchar(200) not null
SourceSystem nvarchar(100) not null
TargetSystem nvarchar(100) not null
CreatedUtc datetimeoffset not null
UpdatedUtc datetimeoffset null
Status nvarchar(50) not null
```

### MigrationRun

Represents one execution attempt or dry run.

```sql
RunId uniqueidentifier primary key
ProjectId uniqueidentifier not null
WorkspaceId nvarchar(100) not null
RunName nvarchar(200) not null
DryRun bit not null
Status nvarchar(50) not null
RequestedBy nvarchar(200) null
StartedUtc datetimeoffset null
CompletedUtc datetimeoffset null
CreatedUtc datetimeoffset not null
```

### ManifestItem

Represents normalized source manifest/inventory rows.

```sql
ManifestItemId bigint identity primary key
ProjectId uniqueidentifier not null
WorkspaceId nvarchar(100) not null
SourceSystem nvarchar(100) not null
SourceObjectId nvarchar(500) not null
SourceObjectVersion nvarchar(200) null
SourcePath nvarchar(2000) null
SourceUri nvarchar(2000) null
SourceLastModifiedUtc datetimeoffset null
ContentHash nvarchar(200) null
IsInScope bit not null
RawArtifactObjectKey nvarchar(2000) null
NormalizedMetadataJson nvarchar(max) null
CreatedUtc datetimeoffset not null
UpdatedUtc datetimeoffset null
```

### MigrationWorkItem

Represents run-specific execution state for a manifest item.

```sql
WorkItemId bigint identity primary key
RunId uniqueidentifier not null
ProjectId uniqueidentifier not null
ManifestItemId bigint not null
WorkspaceId nvarchar(100) not null
Status nvarchar(50) not null
AttemptCount int not null
LeaseId nvarchar(200) null
LeaseExpiresUtc datetimeoffset null
NextAttemptUtc datetimeoffset null
LastAttemptUtc datetimeoffset null
CompletedUtc datetimeoffset null
LastErrorCode nvarchar(200) null
LastErrorMessage nvarchar(max) null
CreatedUtc datetimeoffset not null
UpdatedUtc datetimeoffset null
```

### MigrationObjectMap

Represents durable source-to-target identity mapping.

```sql
ObjectMapId bigint identity primary key
ProjectId uniqueidentifier not null
WorkspaceId nvarchar(100) not null
SourceSystem nvarchar(100) not null
SourceObjectId nvarchar(500) not null
TargetSystem nvarchar(100) not null
TargetObjectId nvarchar(500) null
TargetPublicId nvarchar(500) null
TargetUri nvarchar(2000) null
TargetVersion nvarchar(200) null
MappingStatus nvarchar(50) not null
FirstMappedUtc datetimeoffset null
LastMappedUtc datetimeoffset null
LastVerifiedUtc datetimeoffset null
```

### WorkItemAttempt

Represents each execution attempt.

```sql
AttemptId bigint identity primary key
WorkItemId bigint not null
RunId uniqueidentifier not null
AttemptNumber int not null
StartedUtc datetimeoffset not null
CompletedUtc datetimeoffset null
Status nvarchar(50) not null
WorkerId nvarchar(200) null
ErrorCode nvarchar(200) null
ErrorMessage nvarchar(max) null
DiagnosticArtifactObjectKey nvarchar(2000) null
```

### WorkItemFailure

Represents failure details and retry classification.

```sql
FailureId bigint identity primary key
WorkItemId bigint not null
RunId uniqueidentifier not null
FailureCategory nvarchar(100) not null
FailureCode nvarchar(200) null
FailureMessage nvarchar(max) not null
IsRetryable bit not null
RetryAfterUtc datetimeoffset null
FailureArtifactObjectKey nvarchar(2000) null
CreatedUtc datetimeoffset not null
```

## Important indexes

Recommended starting indexes:

```sql
create index IX_ManifestItem_Project_SourceObject
on ManifestItem(ProjectId, SourceSystem, SourceObjectId);

create index IX_WorkItem_Run_Status
on MigrationWorkItem(RunId, Status);

create index IX_WorkItem_Lease
on MigrationWorkItem(Status, LeaseExpiresUtc)
where Status in ('Queued', 'RetryPending', 'Leased');

create index IX_ObjectMap_Project_Source
on MigrationObjectMap(ProjectId, SourceSystem, SourceObjectId);

create index IX_ObjectMap_Project_Target
on MigrationObjectMap(ProjectId, TargetSystem, TargetObjectId);

create index IX_Attempt_WorkItem
on WorkItemAttempt(WorkItemId, AttemptNumber);

create index IX_Failure_Run_Retryable
on WorkItemFailure(RunId, IsRetryable, RetryAfterUtc);
```

## Status examples

Manifest item status:

```text
Discovered
InScope
OutOfScope
Invalid
Ready
Blocked
```

Work item status:

```text
Queued
Leased
Running
Succeeded
Failed
RetryPending
Skipped
Cancelled
DeadLettered
```

Mapping status:

```text
Unmapped
Mapped
Verified
Conflict
Stale
Deleted
```

## Key principle

The table should preserve both source and target identifiers.

At minimum:

```text
SourceSystem
SourceObjectId
SourcePath / SourceUri
TargetSystem
TargetObjectId
TargetPublicId / TargetUri
```

This is required for retries, reconciliation, reruns, reporting, and cutover validation.
