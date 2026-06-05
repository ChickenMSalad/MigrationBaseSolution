# P1 Set 014 — Health + Operational Diagnostics Surface

## Purpose

P1 Set 014 adds explicit operational health endpoints for runtime probes and operator diagnostics.

This is additive and does not change storage, queue, auth, worker, or deployment behavior.

## Added files

- `src/Migration.Admin.Api/Contracts/OperationalHealthContracts.cs`
- `src/Migration.Admin.Api/Endpoints/OperationalHealthEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/operationalHealth.ts`
- `docs/cloud-roadmap-cleanup/P1_SET_014_OPERATIONAL_HEALTH.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Only adds:

```csharp
app.MapOperationalHealthEndpoints();
```

## New routes

```http
GET /health/live
GET /health/ready
GET /health/cloud
```

## Intent

- `/health/live` is a simple process liveness probe.
- `/health/ready` reports local readiness shape.
- `/health/cloud` reports cloud-oriented operational health warnings.

## Safe output

No secrets are returned.

## Validation

Run:

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then start the Admin API and call:

```powershell
Invoke-RestMethod http://localhost:5173/health/live
Invoke-RestMethod http://localhost:5173/health/ready
Invoke-RestMethod http://localhost:5173/health/cloud
```
