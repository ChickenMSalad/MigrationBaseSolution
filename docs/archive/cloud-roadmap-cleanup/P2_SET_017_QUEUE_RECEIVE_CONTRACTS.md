# P2 Set 017 — Queue Receive Contracts

## Purpose

P2 Set 017 adds queue receive/provider contracts and Azure Queue receive scaffolding.

This is additive and does not switch the worker to the new receive abstraction yet.

## Added files

- `src/Migration.ControlPlane/Queues/IQueueReceiveProvider.cs`
- `src/Migration.ControlPlane/Queues/InMemoryQueueReceiveProvider.cs`
- `src/Migration.ControlPlane/Queues/NullQueueReceiveProvider.cs`
- `src/Migration.ControlPlane/Queues/AzureQueueReceiveProvider.cs`
- `src/Migration.ControlPlane/Queues/QueueReceiveRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/QueueReceiveDiagnosticsEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueReceive.ts`
- `tools/test/smoke-queue-receive.ps1`
- `tools/test/smoke-queue-receive.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_017_QUEUE_RECEIVE_CONTRACTS.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
builder.Services.AddQueueReceiveProvider(builder.Configuration);
api.MapQueueReceiveDiagnosticsEndpoints();
```

## New API routes

```http
GET /api/cloud/queue/receive/provider
POST /api/cloud/queue/receive/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Current local/unconfigured validation:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-receive.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured
```
