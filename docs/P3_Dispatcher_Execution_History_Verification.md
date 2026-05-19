# P3 Dispatcher Execution History Verification

## Purpose

This smoke coverage verifies the complete dispatcher history path:

```text
readiness endpoint
dispatcher run-once endpoint
history list endpoint
history detail endpoint
```

## Verification commands

```powershell
./scripts/operational-dispatcher-execution-history-route-check.ps1 -BaseUrl "https://localhost:55436"
./scripts/operational-dispatcher-execution-history-readiness-smoke-test.ps1 -BaseUrl "https://localhost:55436"
./scripts/operational-dispatcher-execution-history-e2e-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```

## Expected behavior

Even if no work items are available, `run-once` should still record an execution history row.

Typical result when no work is available:

```text
LeasedCount: 0
CompletedCount: 0
FailedCount: 0
Outcome: Completed
Message: No eligible work items were leased.
```

That is valid because the dispatcher cycle itself completed successfully.

## Why this matters

Before enabling continuous dispatcher automation, we need durable visibility into every dispatcher cycle.
