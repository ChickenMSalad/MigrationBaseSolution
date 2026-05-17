# P2 Set 015 — Queue Dispatch Contracts

## Purpose

P2 Set 015 adds the queue dispatch provider abstraction and an in-memory local implementation.

This does not replace the queue worker yet and does not send messages to Azure Queue yet.

## Added files

- `src/Migration.ControlPlane/Queues/IQueueDispatchProvider.cs`
- `src/Migration.ControlPlane/Queues/InMemoryQueueDispatchProvider.cs`
- `src/Migration.ControlPlane/Queues/NullQueueDispatchProvider.cs`
- `src/Migration.ControlPlane/Queues/QueueDispatchRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/QueueDispatchDiagnosticsEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueDispatch.ts`
- `tools/test/smoke-queue-dispatch.ps1`
- `tools/test/smoke-queue-dispatch.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_015_QUEUE_DISPATCH_CONTRACTS.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
builder.Services.AddQueueDispatchProvider(builder.Configuration);
api.MapQueueDispatchDiagnosticsEndpoints();
```

## New API routes

```http
GET /api/cloud/queue/dispatch/provider
POST /api/cloud/queue/dispatch/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API, then:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-dispatch.ps1 -BaseUrl http://localhost:5173
```
