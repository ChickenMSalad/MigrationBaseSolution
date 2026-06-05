# P2 Set 024 — Queue Executor Coordinator

## Purpose

P2 Set 024 adds a dry-run queue executor coordinator that combines:

- queue receive provider
- queue execution planner
- queue failure handler
- poison handling plan

This still does not execute real migrations from the worker loop.

## Added files

- `src/Migration.ControlPlane/Queues/QueueExecutorCoordinatorContracts.cs`
- `src/Migration.ControlPlane/Queues/IQueueExecutorCoordinator.cs`
- `src/Migration.ControlPlane/Queues/QueueExecutorCoordinator.cs`
- `src/Migration.ControlPlane/Queues/QueueExecutorCoordinatorRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/QueueExecutorCoordinatorEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueExecutorCoordinator.ts`
- `tools/test/smoke-queue-executor-coordinator.ps1`
- `tools/test/smoke-queue-executor-coordinator.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_024_QUEUE_EXECUTOR_COORDINATOR.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
builder.Services.AddQueueExecutorCoordinator(builder.Configuration);
api.MapQueueExecutorCoordinatorEndpoints();
```

## New API routes

```http
GET /api/cloud/queue/executor-coordinator/options
POST /api/cloud/queue/executor-coordinator/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-executor-coordinator.ps1 -BaseUrl http://localhost:5173
```
