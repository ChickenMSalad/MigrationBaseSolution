# P3 Execution Boundaries

## Purpose

This document defines the system boundaries for P3 execution.

## SQL Server

SQL Server owns durable operational state:

- manifests
- work items
- runs
- leases
- retries
- failures
- target mappings
- checkpoints
- reconciliation state
- report summaries

SQL is the answer to: "What is the current truth of this migration?"

## Blob Storage

Blob Storage owns large artifacts:

- source exports
- uploaded manifests
- generated reports
- raw sidecars
- large diagnostic payloads
- binary staging
- archival evidence

Blob is the answer to: "Where is the large file or raw evidence?"

## Queue system

Queues coordinate execution.

Queues should not be the only source of truth. A queue message should point to SQL state.

A queue message should generally contain:

```text
WorkspaceId
ProjectId
RunId
WorkItemId or BatchId
MessageType
IdempotencyKey
CorrelationId
```

The worker should load full execution context from SQL.

## Worker

Workers execute.

Workers should:

- claim/lease work items from SQL
- load artifacts from Blob/source systems
- upsert target systems
- update SQL state
- emit audit events
- emit telemetry
- honor cancellation
- avoid completing queue messages until SQL is updated

## Admin API

Admin API controls and reports.

The API should:

- create projects
- load manifests
- validate mappings
- create runs
- enqueue work
- expose readiness
- expose governance
- expose reports
- never return raw secret values

## Key Vault

Key Vault owns secrets.

SQL should store references, not raw secret values.

## Application Insights / Telemetry

Telemetry owns distributed runtime observation:

- request traces
- worker traces
- dependency timing
- exception telemetry
- correlation
- performance metrics

Telemetry should not replace SQL operational state.

## Final boundary summary

```text
SQL Server       = operational truth
Blob Storage     = large artifacts
Queue            = coordination
Worker           = execution
Admin API        = control/governance/reporting
Key Vault        = secrets
Telemetry        = distributed observation
```
