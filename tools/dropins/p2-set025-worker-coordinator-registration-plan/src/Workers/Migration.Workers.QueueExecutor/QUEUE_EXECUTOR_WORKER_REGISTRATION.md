# Queue Executor Worker Registration Plan

## Purpose

This file documents the planned wiring for the queue executor worker.

The worker should eventually compose:

- `IQueueReceiveProvider`
- `IQueueExecutionPlanner`
- `IQueueFailureHandler`
- `IQueueExecutorCoordinator`
- `QueueWorkerLoopService`

## Current safety posture

The loop remains disabled unless explicitly configured:

```json
{
  "QueueWorkerLoop": {
    "Enabled": false
  },
  "QueueExecutorCoordinator": {
    "DryRun": true,
    "CompleteMessages": false
  }
}
```

## Future startup wiring

When the worker bootstrap is ready for live queue execution, it should include equivalent registrations:

```csharp
services.AddCloudStoragePathResolution(configuration);
services.AddCloudBinaryStorage(configuration);
services.AddArtifactStorage();
services.AddArtifactManifestIndex();

services.AddQueueReceiveProvider(configuration);
services.AddQueueFailureHandling();
services.AddQueueExecutionPlanning();
services.AddQueueExecutorCoordinator(configuration);

services.AddHostedService<QueueWorkerLoopService>();
```

## Do not enable live execution until

- Azure Queue receive is configured.
- Artifact storage is configured.
- Failure artifact handling is validated.
- Idempotency and lease behavior are enforced.
- Message completion behavior is explicitly selected.
