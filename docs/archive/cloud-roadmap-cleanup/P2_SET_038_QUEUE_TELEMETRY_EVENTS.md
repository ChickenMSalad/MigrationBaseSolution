# P2 Set 038 — Queue Telemetry Events

## Purpose

P2 Set 038 adds queue telemetry event names/factory helpers and diagnostics probes.

This allows the queue execution stack to emit normalized telemetry events through `ITelemetryEventWriter` without changing live queue runtime behavior.

## Added files

- `src/Migration.ControlPlane/Telemetry/QueueTelemetryEventNames.cs`
- `src/Migration.ControlPlane/Telemetry/QueueTelemetryEventFactory.cs`
- `src/Migration.Admin.Api/Endpoints/QueueTelemetryEventEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueTelemetryEvents.ts`
- `tools/test/smoke-queue-telemetry-events.ps1`
- `tools/test/smoke-queue-telemetry-events.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_038_QUEUE_TELEMETRY_EVENTS.md`

## Program.cs addition

```csharp
api.MapQueueTelemetryEventEndpoints();
```

## New API routes

```http
GET /api/cloud/queue/telemetry/event-names
POST /api/cloud/queue/telemetry/probe
```
