# P2 Set 033 — Queue Audit Events

## Purpose

P2 Set 033 adds queue audit event names/factory helpers and diagnostics probes.

This allows the queue execution stack to emit normalized audit events through `IAuditEventWriter` without changing live queue runtime behavior.

## Added files

- `src/Migration.ControlPlane/Audit/QueueAuditEventNames.cs`
- `src/Migration.ControlPlane/Audit/QueueAuditEventFactory.cs`
- `src/Migration.Admin.Api/Endpoints/QueueAuditEventEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueAuditEvents.ts`
- `tools/test/smoke-queue-audit-events.ps1`
- `tools/test/smoke-queue-audit-events.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_033_QUEUE_AUDIT_EVENTS.md`

## Program.cs addition

```csharp
api.MapQueueAuditEventEndpoints();
```

## New API routes

```http
GET /api/cloud/queue/audit/event-names
POST /api/cloud/queue/audit/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-audit-events.ps1 -BaseUrl http://localhost:5173
```
