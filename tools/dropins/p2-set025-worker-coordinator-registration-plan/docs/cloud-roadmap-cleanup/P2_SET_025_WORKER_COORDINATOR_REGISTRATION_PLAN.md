# P2 Set 025 — Worker Coordinator Registration Plan

## Purpose

P2 Set 025 adds worker-side registration planning for the queue executor coordinator.

This does not enable live queue execution.

## Added files

- `src/Workers/Migration.Workers.QueueExecutor/QueueExecutorWorkerRegistrationPlan.cs`
- `src/Workers/Migration.Workers.QueueExecutor/QUEUE_EXECUTOR_WORKER_REGISTRATION.md`
- `tools/test/smoke-worker-coordinator-registration-plan.ps1`
- `tools/test/smoke-worker-coordinator-registration-plan.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_025_WORKER_COORDINATOR_REGISTRATION_PLAN.md`

## Why this matters

The queue execution stack now has contracts for:

- dispatch
- receive
- worker loop
- poison handling
- failure artifacts
- execution planning
- coordinator dry-run polling

Before enabling real worker execution, the worker project needs a clear registration plan and smoke validation.

## Validation

```powershell
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-worker-coordinator-registration-plan.ps1
```
