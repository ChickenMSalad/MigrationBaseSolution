# P2 Set 022 — Queue Failure Handler

## Purpose

P2 Set 022 adds a queue failure handler service that writes failure artifacts through the artifact storage layer.

This still does not wire failure handling into live worker execution.

## Added files

- `src/Migration.ControlPlane/Queues/IQueueFailureHandler.cs`
- `src/Migration.ControlPlane/Queues/QueueFailureHandler.cs`
- `src/Migration.ControlPlane/Queues/QueueFailureHandlerRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/QueueFailureHandlerEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueFailureHandler.ts`
- `tools/test/smoke-queue-failure-handler.ps1`
- `tools/test/smoke-queue-failure-handler.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_022_QUEUE_FAILURE_HANDLER.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
builder.Services.AddQueueFailureHandling();
api.MapQueueFailureHandlerEndpoints();
```

## New API route

```http
POST /api/cloud/queue/failure-handler/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-failure-handler.ps1 -BaseUrl http://localhost:5173
```
