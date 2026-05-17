# P2 Set 028 — Queue Execution Readiness

## Purpose

P2 Set 028 adds a queue execution readiness rollup that combines:

- dispatch provider
- receive provider
- worker loop plan
- poison handling plan
- queue execution observability

This remains read-only and does not enable live execution.

## Added files

- `src/Migration.ControlPlane/Queues/QueueExecutionReadinessContracts.cs`
- `src/Migration.ControlPlane/Queues/IQueueExecutionReadinessService.cs`
- `src/Migration.ControlPlane/Queues/QueueExecutionReadinessService.cs`
- `src/Migration.ControlPlane/Queues/QueueExecutionReadinessRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/QueueExecutionReadinessEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueExecutionReadiness.ts`
- `tools/test/smoke-queue-execution-readiness.ps1`
- `tools/test/smoke-queue-execution-readiness.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_028_QUEUE_EXECUTION_READINESS.md`

## Program.cs additions

```csharp
builder.Services.AddQueueExecutionReadiness();
api.MapQueueExecutionReadinessEndpoints();
```

## Endpoint

```http
GET /api/cloud/queue/execution-readiness
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-execution-readiness.ps1 -BaseUrl http://localhost:5173
```
