# P2 Set 020 — Queue Poison Handling Planning

## Purpose

P2 Set 020 adds poison/dead-letter planning contracts and diagnostics for queue processing.

This does not change dispatch, receive, or worker behavior yet.

## Added files

- `src/Migration.ControlPlane/Queues/QueuePoisonMessageContracts.cs`
- `src/Migration.ControlPlane/Queues/QueuePoisonHandlingPlanner.cs`
- `src/Migration.Admin.Api/Endpoints/QueuePoisonHandlingEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queuePoisonHandling.ts`
- `tools/test/smoke-queue-poison-handling.ps1`
- `tools/test/smoke-queue-poison-handling.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_020_QUEUE_POISON_HANDLING.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
api.MapQueuePoisonHandlingEndpoints();
```

## New API routes

```http
GET /api/cloud/queue/poison-handling
GET /api/cloud/queue/poison-handling/recommendation
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-poison-handling.ps1 -BaseUrl http://localhost:5173
```
