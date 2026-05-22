# P4.9 Operational Run Coordinator

Adds the first SQL-backed run coordination layer on top of the operational SQL backbone, SQL manifest provider, SQL work-item queue, Service Bus dispatcher/executor, and lease coordination sets.

## Scope

- Application-level run coordinator contracts.
- SQL-backed run state transition coordinator.
- Fan-out from SQL manifest rows into durable operational work items.
- Cancellation request persistence.
- Completion evaluation from SQL queue summaries.
- Admin API endpoints under `/api/operational/sql-backbone/runs`.

## Non-goals

- No connector-specific execution logic.
- No UI changes.
- No tenant model yet.
- No speculative DTO expansion beyond the coordinator contract.

## Runtime flow

1. Admin API requests run start.
2. SQL run row moves to `Running`.
3. Coordinator reads eligible SQL manifest rows.
4. Coordinator enqueues durable SQL work items.
5. Dispatcher/Executor sets process work.
6. Coordinator evaluates completion from work-item summary.

## Validation

```powershell
./patches/P4.9-Install-OperationalRunCoordinator.ps1 -WhatIf
./patches/P4.9-Install-OperationalRunCoordinator.ps1 -Apply
./patches/P4.9-Validate-OperationalRunCoordinator.ps1

dotnet restore
dotnet build
```
