# P2 Set 016 — Azure Queue Dispatch Scaffold

## Purpose

P2 Set 016 adds an Azure Storage Queue-backed dispatch provider behind the queue dispatch abstraction.

It only activates when `MigrationRunQueue:Provider` is `AzureQueue` and storage account/service URI/connection string configuration exists.

## Added files

- `src/Migration.ControlPlane/Queues/AzureQueueDispatchOptions.cs`
- `src/Migration.ControlPlane/Queues/AzureQueueDispatchProvider.cs`
- `tools/test/smoke-azure-queue-dispatch.ps1`
- `tools/test/smoke-azure-queue-dispatch.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_016_AZURE_QUEUE_DISPATCH.md`

## Replaced file

- `src/Migration.ControlPlane/Queues/QueueDispatchRegistrationExtensions.cs`

## Modified files

- `src/Migration.ControlPlane/Migration.ControlPlane.csproj`
- `Directory.Packages.props`

Adds package:

```xml
Azure.Storage.Queues
```

## Validation

```powershell
dotnet restore .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Local/unconfigured validation:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-azure-queue-dispatch.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured
```
