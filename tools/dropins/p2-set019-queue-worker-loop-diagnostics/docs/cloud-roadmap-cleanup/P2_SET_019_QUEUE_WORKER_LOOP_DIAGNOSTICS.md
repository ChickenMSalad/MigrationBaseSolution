# P2 Set 019 — Queue Worker Loop Diagnostics

## Purpose

P2 Set 019 adds Admin API diagnostics for the queue worker polling loop scaffold.

This does not enable the worker loop and does not change queue execution behavior.

## Added files

- `src/Migration.Admin.Api/Endpoints/QueueWorkerLoopDiagnosticsEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueWorkerLoop.ts`
- `tools/test/smoke-queue-worker-loop-diagnostics.ps1`
- `tools/test/smoke-queue-worker-loop-diagnostics.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_019_QUEUE_WORKER_LOOP_DIAGNOSTICS.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
api.MapQueueWorkerLoopDiagnosticsEndpoints();
```

## New API routes

```http
GET /api/cloud/queue/worker-loop
GET /api/cloud/queue/worker-loop/safety
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API, then:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-worker-loop-diagnostics.ps1 -BaseUrl http://localhost:5173
```
