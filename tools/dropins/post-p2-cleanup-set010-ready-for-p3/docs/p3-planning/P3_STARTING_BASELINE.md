# P3 Starting Baseline

## Status

P2 is complete.

Post-P2 cleanup has established the baseline needed to begin P3 intentionally.

## Current validated platform posture

Expected local development posture:

```text
Overall status      : production-diagnostics-ready
Diagnostics ready   : True
Production ready    : True
Live queue ready    : False
Operational mode    : local-development
```

This is the correct posture before P3.

## What exists now

The repo now has:

- reusable migration connectors and hosts
- generic runner infrastructure
- Admin API control-plane surface
- queue contract and governance model
- queue execution readiness diagnostics
- audit persistence abstraction
- telemetry abstraction
- operational readiness rollups
- auth policy readiness
- endpoint policy inventory
- credential access policy readiness
- production safety gates
- operational mode reporting
- queue execution governance
- P2 readiness reporting
- full P2 validation harness
- post-P2 maintenance/inventory scripts
- P3 SQL operational model docs

## P3 architectural commitments

### SQL Server is the operational truth

SQL Server will own:

- normalized manifest rows
- migration work items
- run state
- source identifiers
- target identifiers
- retry state
- failure state
- checkpoint state
- reconciliation state
- report summaries

CSV and Excel are import/export/review artifacts only.

### Blob Storage owns large artifacts

Blob Storage will own:

- binaries
- sidecar metadata
- generated reports
- raw source exports
- uploaded manifests
- diagnostic payload artifacts

### Queue system coordinates execution

Queue messages should point to SQL state. Queue messages should not be the only durable source of truth.

### Workers execute

Workers claim SQL work items, execute source/target operations, update SQL, emit audit/telemetry, and complete queue messages only after durable state is updated.

### Admin API governs

The Admin API creates projects, loads manifests, validates mappings, starts runs, exposes status, and enforces governance.

## P3 should not start by enabling live execution

Recommended P3 order:

1. SQL operational schema/contracts
2. SQL manifest ingestion
3. SQL work item creation
4. SQL-backed run state
5. worker claim/lease model
6. auth enforcement rollout
7. live queue execution behind governance
8. durable audit query model
9. telemetry provider integration
10. operational UI surfaces

## P3 safety rules

Do not enable destructive live execution until:

- auth enforcement is active in production-like mode
- SQL operational state exists
- audit persistence is durable
- telemetry is configured
- queue governance allows live execution
- message completion is explicitly enabled
- manual approval is recorded
- rollback/retry/reconciliation behavior is tested

## Pre-P3 validation commands

Run these before starting P3 code work:

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\maintenance\validate-post-p2-cleanup.ps1
powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured
```

## P3 first recommended implementation set

Recommended first P3 implementation set:

```text
P3 Set 001 — SQL Operational Store Contracts
```

That set should add contracts only, not full execution:

- `MigrationRun`
- `ManifestItem`
- `MigrationWorkItem`
- `MigrationObjectMap`
- `WorkItemAttempt`
- `WorkItemFailure`
- repository interfaces
- status enums
- SQL store readiness diagnostics

The first P3 code should define the durable operational model before worker execution is implemented.
