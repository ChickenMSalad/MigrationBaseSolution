# P2 Set 018 — Queue Worker Polling Loop Scaffold

## Purpose

P2 Set 018 adds a worker-side polling loop scaffold over the new queue receive provider.

The loop is disabled by default and dry-run by default.

## Added files

- `src/Migration.ControlPlane/Queues/QueueWorkerLoopOptions.cs`
- `src/Migration.ControlPlane/Queues/QueueWorkerLoopDescriptor.cs`
- `src/Migration.ControlPlane/Queues/QueueWorkerLoopPlanner.cs`
- `src/Workers/Migration.Workers.QueueExecutor/QueueWorkerLoopService.cs`
- `tools/test/smoke-queue-worker-loop.ps1`
- `tools/test/smoke-queue-worker-loop.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_018_QUEUE_WORKER_LOOP_SCAFFOLD.md`

## Modified worker startup

The worker project should register:

```csharp
services.AddQueueReceiveProvider(configuration);
services.AddHostedService<QueueWorkerLoopService>();
```

Only if its current Program/startup shape supports that safely.

## Configuration

```json
{
  "QueueWorkerLoop": {
    "Enabled": false,
    "DryRun": true,
    "MaxMessages": 1,
    "PollIntervalSeconds": 10,
    "VisibilityTimeoutSeconds": 300,
    "CompleteMessages": false
  }
}
```

## Validation

```powershell
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-worker-loop.ps1
```
