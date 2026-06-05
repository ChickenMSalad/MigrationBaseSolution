# P2 Set 039 — Cloud Operation Telemetry

## Purpose

P2 Set 039 adds normalized telemetry helpers for cloud platform operations.

This mirrors the cloud operation audit event layer and remains additive only.

## Added files

- `src/Migration.ControlPlane/Telemetry/CloudOperationTelemetryEventNames.cs`
- `src/Migration.ControlPlane/Telemetry/CloudOperationTelemetryEventFactory.cs`
- `src/Migration.Admin.Api/Endpoints/CloudOperationTelemetryEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/cloudOperationTelemetry.ts`
- `tools/test/smoke-cloud-operation-telemetry.ps1`
- `tools/test/smoke-cloud-operation-telemetry.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_039_CLOUD_OPERATION_TELEMETRY.md`

## Program.cs addition

```csharp
api.MapCloudOperationTelemetryEndpoints();
```

## New API routes

```http
GET /api/cloud/telemetry/operation/event-names
POST /api/cloud/telemetry/operation/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-cloud-operation-telemetry.ps1 -BaseUrl http://localhost:5173
```
