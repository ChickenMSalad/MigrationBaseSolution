# P2 Set 047 — Production Safety Gate Aggregation

## Purpose

P2 Set 047 adds a read-only production safety gate rollup across:

- auth policy readiness
- credential access policy readiness
- operational readiness
- live queue execution readiness

This does not enforce production gates yet.

## Added files

- `src/Migration.ControlPlane/Operations/ProductionSafetyGateContracts.cs`
- `src/Migration.ControlPlane/Operations/IProductionSafetyGateService.cs`
- `src/Migration.ControlPlane/Operations/ProductionSafetyGateService.cs`
- `src/Migration.ControlPlane/Operations/ProductionSafetyGateRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/ProductionSafetyGateEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/productionSafetyGates.ts`
- `tools/test/smoke-production-safety-gates.ps1`
- `tools/test/smoke-production-safety-gates.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_047_PRODUCTION_SAFETY_GATES.md`

## Program.cs additions

```csharp
builder.Services.AddProductionSafetyGates();
api.MapProductionSafetyGateEndpoints();
```

## Endpoint

```http
GET /api/cloud/operations/production-safety-gates
```
