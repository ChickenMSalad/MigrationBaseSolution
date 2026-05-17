# P2 Set 014 — Queue Serialization + Idempotency Planning

## Purpose

P2 Set 014 adds queue envelope serialization helpers and deterministic idempotency/lease resource builders.

This is still additive and does not switch the worker to a new queue processor yet.

## Added files

- `src/Migration.ControlPlane/Queues/QueueMessageSerialization.cs`
- `src/Migration.ControlPlane/Queues/QueueIdempotencyKeyBuilder.cs`
- `src/Migration.Admin.Api/Endpoints/QueueIdempotencyEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueIdempotency.ts`
- `tools/test/smoke-queue-idempotency.ps1`
- `tools/test/smoke-queue-idempotency.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_014_QUEUE_SERIALIZATION_IDEMPOTENCY.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
api.MapQueueIdempotencyEndpoints();
```

## New API routes

```http
GET /api/cloud/queue/idempotency
POST /api/cloud/queue/envelope/serialize
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API, then:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-idempotency.ps1 -BaseUrl http://localhost:5173
```
