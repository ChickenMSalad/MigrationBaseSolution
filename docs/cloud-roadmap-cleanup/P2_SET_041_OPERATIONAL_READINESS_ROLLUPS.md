# P2 Set 041 — Operational Readiness Rollups

## Purpose

P2 Set 041 adds a read-only operational readiness rollup across:

- audit persistence
- telemetry provider
- queue execution readiness

This does not enable live queue execution or change runtime behavior.

## Added files

- `src/Migration.ControlPlane/Operations/OperationalReadinessContracts.cs`
- `src/Migration.ControlPlane/Operations/IOperationalReadinessService.cs`
- `src/Migration.ControlPlane/Operations/OperationalReadinessService.cs`
- `src/Migration.ControlPlane/Operations/OperationalReadinessRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/OperationalReadinessEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/operationalReadiness.ts`
- `tools/test/smoke-operational-readiness-rollups.ps1`
- `tools/test/smoke-operational-readiness-rollups.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_041_OPERATIONAL_READINESS_ROLLUPS.md`

## Program.cs additions

```csharp
using Migration.ControlPlane.Operations;

builder.Services.AddOperationalReadiness();
api.MapOperationalReadinessEndpoints();
```

## Endpoint

```http
GET /api/cloud/operations/readiness
```
