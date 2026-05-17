# P2 Set 013 — Queue Message Contracts

## Purpose

P2 Set 013 introduces a normalized queue envelope contract and queue provider diagnostics layer.

This is the foundation for durable/idempotent queue execution.

## Added files

- `src/Migration.ControlPlane/Queues/QueueMessageContracts.cs`
- `src/Migration.ControlPlane/Queues/QueueMessageEnvelopeFactory.cs`
- `src/Migration.Admin.Api/Endpoints/QueueContractDiagnosticsEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueContracts.ts`
- `tools/test/smoke-queue-contracts.ps1`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
api.MapQueueContractDiagnosticsEndpoints();
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-contracts.ps1
```
