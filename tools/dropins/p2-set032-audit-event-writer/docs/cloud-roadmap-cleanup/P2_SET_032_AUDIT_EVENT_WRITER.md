# P2 Set 032 — Audit Event Writer

## Purpose

P2 Set 032 adds an audit event writer service on top of audit persistence.

This gives cloud/queue/runtime components a simple normalized writer without coupling directly to persistence provider details.

## Added files

- `src/Migration.ControlPlane/Audit/IAuditEventWriter.cs`
- `src/Migration.ControlPlane/Audit/AuditEventWriter.cs`
- `src/Migration.ControlPlane/Audit/AuditEventWriterRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/AuditEventWriterEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/auditEventWriter.ts`
- `tools/test/smoke-audit-event-writer.ps1`
- `tools/test/smoke-audit-event-writer.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_032_AUDIT_EVENT_WRITER.md`

## Program.cs additions

```csharp
builder.Services.AddAuditEventWriter();
api.MapAuditEventWriterEndpoints();
```

## New API route

```http
POST /api/cloud/audit/writer/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-audit-event-writer.ps1 -BaseUrl http://localhost:5173
```
