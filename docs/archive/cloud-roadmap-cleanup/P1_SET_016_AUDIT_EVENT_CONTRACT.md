# P1 Set 016 — Audit Event Contract

## Purpose

P1 Set 016 adds a safe audit event contract endpoint.

This is contract/planning only. It does not persist audit events yet and does not change runtime behavior.

## Added files

- `src/Migration.Admin.Api/Contracts/AuditEventContractContracts.cs`
- `src/Migration.Admin.Api/Endpoints/AuditEventContractEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/auditEventContract.ts`
- `docs/cloud-roadmap-cleanup/P1_SET_016_AUDIT_EVENT_CONTRACT.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Only adds:

```csharp
api.MapAuditEventContractEndpoints();
```

## New API route

```http
GET /api/cloud/audit/event-contract
```

## Contract scope

The endpoint defines:

- supported audit event types
- required audit properties
- redacted property names
- intended provider kind
- audit storage root planning
- workspace/tenant context
- warnings

## Why this matters

Before auth, tenancy enforcement, and cloud storage are implemented, we need a stable audit event contract for:

- project changes
- run actions
- credential actions
- artifact actions
- workspace resolution
- future compliance/audit exports

## Validation

Run:

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then start the Admin API and call:

```powershell
Invoke-RestMethod http://localhost:5173/api/cloud/audit/event-contract
```
