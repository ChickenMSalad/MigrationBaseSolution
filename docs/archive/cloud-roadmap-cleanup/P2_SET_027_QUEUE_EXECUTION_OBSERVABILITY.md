# P2 Set 027 — Queue Execution Observability

## Purpose

Adds queue execution observability snapshots and diagnostics endpoints.

This remains read-only and does not enable live queue execution.

## Added files

- QueueExecutionObservabilityContracts.cs
- IQueueExecutionObservabilityService.cs
- QueueExecutionObservabilityService.cs
- QueueExecutionObservabilityRegistrationExtensions.cs
- QueueExecutionObservabilityEndpointExtensions.cs
- queueExecutionObservability.ts
- smoke-queue-execution-observability.ps1

## Program.cs additions

```csharp
builder.Services.AddQueueExecutionObservability();
api.MapQueueExecutionObservabilityEndpoints();
```

## Endpoint

```http
GET /api/cloud/queue/execution-observability
```
