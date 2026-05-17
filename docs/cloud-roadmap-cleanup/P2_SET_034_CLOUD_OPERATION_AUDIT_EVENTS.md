# P2 Set 034 — Cloud Operation Audit Events

## Purpose

P2 Set 034 adds normalized audit event helpers for cloud platform operations.

This extends audit coverage beyond the queue stack without changing existing runtime behavior.

## Added files

- `src/Migration.ControlPlane/Audit/CloudOperationAuditEventNames.cs`
- `src/Migration.ControlPlane/Audit/CloudOperationAuditEventFactory.cs`
- `src/Migration.Admin.Api/Endpoints/CloudOperationAuditEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/cloudOperationAudit.ts`
- `tools/test/smoke-cloud-operation-audit.ps1`
- `tools/test/smoke-cloud-operation-audit.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_034_CLOUD_OPERATION_AUDIT_EVENTS.md`

## Program.cs addition

```csharp
api.MapCloudOperationAuditEndpoints();
```

## New API routes

```http
GET /api/cloud/audit/operation/event-names
POST /api/cloud/audit/operation/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-cloud-operation-audit.ps1 -BaseUrl http://localhost:5173
```
