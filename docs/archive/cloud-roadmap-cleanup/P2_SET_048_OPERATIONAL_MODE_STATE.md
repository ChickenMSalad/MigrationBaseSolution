# P2 Set 048 — Operational Mode State

## Purpose

P2 Set 048 adds a read-only operational mode/state endpoint.

It summarizes whether the system is running as:

- `local-development`
- `diagnostics-only`
- `production-diagnostics-ready`
- `production-live-queue-ready`

This does not enable or enforce any runtime mode.

## Added files

- `src/Migration.ControlPlane/Operations/OperationalModeContracts.cs`
- `src/Migration.ControlPlane/Operations/IOperationalModeService.cs`
- `src/Migration.ControlPlane/Operations/OperationalModeService.cs`
- `src/Migration.ControlPlane/Operations/OperationalModeRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/OperationalModeEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/operationalMode.ts`
- `tools/test/smoke-operational-mode.ps1`
- `tools/test/smoke-operational-mode.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_048_OPERATIONAL_MODE_STATE.md`

## Program.cs additions

```csharp
builder.Services.AddOperationalMode();
api.MapOperationalModeEndpoints();
```

## Endpoint

```http
GET /api/cloud/operations/mode
```
