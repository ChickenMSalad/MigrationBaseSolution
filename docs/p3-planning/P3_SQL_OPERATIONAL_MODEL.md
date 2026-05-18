# P3 SQL Operational Model

## Core decision

SQL Server should be the durable operational source of truth for cloud/P3 migration execution.

This is not only for logging. SQL becomes the operational ledger that drives:

- migration manifests
- run queues/work items
- processing state
- source identity tracking
- target identity tracking
- failures
- retries
- reruns
- checkpoints
- reconciliation
- reporting

## Non-goals

SQL Server should not store:

- asset binaries
- large sidecar metadata documents
- generated report files
- raw export packages
- large source payload snapshots unless intentionally normalized or summarized

Those belong in Blob Storage or source/target systems.

## Why SQL is required

Large migrations will commonly contain 500k to 1.5M+ manifest rows.

At that scale:

- Excel is not an operational engine
- CSV is not safe for durable run state
- retry management requires indexed status
- reruns require deterministic filters
- source-to-target mapping must be queryable
- reconciliation requires stable identity records
- multiple workers need coordinated leases and state updates
- reporting must not require scanning flat files

## Operational truth model

The SQL database should answer:

- What did the source system contain?
- Which source objects are in scope?
- Which records are ready to migrate?
- Which records are blocked?
- Which records succeeded?
- Which records failed?
- Which target object was created or updated?
- Which source object maps to which target object?
- Which records need retry?
- Which records were skipped and why?
- Which records changed between extract and execution?
- Which run produced which result?

## Artifact boundary

Use Blob Storage for:

- original source exports
- uploaded CSV/Excel files
- generated reports
- logs that are too large for SQL
- sidecar JSON/XML payloads
- binary staging
- full API payload snapshots when needed

Use SQL Server for:

- searchable operational columns
- normalized status
- identity mapping
- failure summaries
- retry state
- queue/work item state
- run metadata
- artifact pointers

## Recommended flow

```text
Extract source inventory
        ↓
Load raw artifact to Blob Storage
        ↓
Normalize manifest rows into SQL
        ↓
Validate and enrich SQL records
        ↓
Create MigrationRun
        ↓
Create MigrationWorkItems
        ↓
Queue execution batches
        ↓
Workers claim/lease SQL work items
        ↓
Workers read binaries/sidecars from source or Blob
        ↓
Workers upsert target
        ↓
Workers persist target identifiers/status/failures
        ↓
Reports generated from SQL + artifact pointers
```

## Relationship to P2

P2 created:

- readiness reporting
- production safety gates
- queue governance
- audit abstraction
- telemetry abstraction
- operational mode
- control-plane diagnostics

P3 should make SQL the durable execution backbone underneath those governance layers.
