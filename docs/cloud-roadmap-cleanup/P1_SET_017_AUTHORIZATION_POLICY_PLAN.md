# P1 Set 017 — Authorization Policy Plan

## Purpose

P1 Set 017 adds a safe authorization policy planning endpoint.

This is contract/planning only. It does not enforce authentication or authorization yet.

## Added files

- `src/Migration.Admin.Api/Contracts/AuthorizationPolicyPlanContracts.cs`
- `src/Migration.Admin.Api/Endpoints/AuthorizationPolicyPlanEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/authorizationPolicyPlan.ts`
- `docs/cloud-roadmap-cleanup/P1_SET_017_AUTHORIZATION_POLICY_PLAN.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Only adds:

```csharp
api.MapAuthorizationPolicyPlanEndpoints();
```

## New API route

```http
GET /api/cloud/auth/policy-plan
```

## Defines

- planned roles
- planned scopes
- route policy mappings
- auth/tenant warnings
- safe authority/audience configured flags

## Important

The endpoint deliberately returns `"<configured>"` instead of actual authority/audience values.

## Validation

Run:

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then start the Admin API and call:

```powershell
Invoke-RestMethod http://localhost:5173/api/cloud/auth/policy-plan
```
