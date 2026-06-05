# P2 Set 023 — Queue Execution Planner

## Purpose

P2 Set 023 adds a safe queue execution planner that maps queue message envelopes to intended execution actions.

This does not execute migrations from the worker loop yet.

## Added files

- `src/Migration.ControlPlane/Queues/QueueExecutionPlanningContracts.cs`
- `src/Migration.ControlPlane/Queues/IQueueExecutionPlanner.cs`
- `src/Migration.ControlPlane/Queues/QueueExecutionPlanner.cs`
- `src/Migration.ControlPlane/Queues/QueueExecutionPlannerRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/QueueExecutionPlannerEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueExecutionPlanner.ts`
- `tools/test/smoke-queue-execution-planner.ps1`
- `tools/test/smoke-queue-execution-planner.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_023_QUEUE_EXECUTION_PLANNER.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
builder.Services.AddQueueExecutionPlanning();
api.MapQueueExecutionPlannerEndpoints();
```

## New API routes

```http
GET /api/cloud/queue/execution-plan/message-types
POST /api/cloud/queue/execution-plan/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-execution-planner.ps1 -BaseUrl http://localhost:5173
```
