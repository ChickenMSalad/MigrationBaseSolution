# P2 Set 044 — Endpoint Policy Inventory

## Purpose

P2 Set 044 adds advisory endpoint-to-policy inventory for Admin API auth hardening.

This does not enforce auth and does not change local development behavior.

## Added files

- `src/Migration.ControlPlane/Auth/EndpointPolicyInventoryContracts.cs`
- `src/Migration.ControlPlane/Auth/IEndpointPolicyInventoryService.cs`
- `src/Migration.ControlPlane/Auth/EndpointPolicyInventoryService.cs`
- `src/Migration.ControlPlane/Auth/EndpointPolicyInventoryRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/EndpointPolicyInventoryEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/endpointPolicyInventory.ts`
- `tools/test/smoke-endpoint-policy-inventory.ps1`
- `tools/test/smoke-endpoint-policy-inventory.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_044_ENDPOINT_POLICY_INVENTORY.md`

## Program.cs additions

```csharp
builder.Services.AddEndpointPolicyInventory();
api.MapEndpointPolicyInventoryEndpoints();
```

## New API route

```http
GET /api/cloud/auth/endpoint-policy-inventory
```
