# P2 Set 043 — Auth Policy Readiness

## Purpose

P2 Set 043 starts the auth hardening slice.

It adds read-only auth policy readiness contracts and diagnostics. It does not enforce auth and does not change local development behavior.

## Added files

- `src/Migration.ControlPlane/Auth/AuthPolicyReadinessContracts.cs`
- `src/Migration.ControlPlane/Auth/IAuthPolicyReadinessService.cs`
- `src/Migration.ControlPlane/Auth/AuthPolicyReadinessService.cs`
- `src/Migration.ControlPlane/Auth/AuthPolicyReadinessRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/AuthPolicyReadinessEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/authPolicyReadiness.ts`
- `tools/test/smoke-auth-policy-readiness.ps1`
- `tools/test/smoke-auth-policy-readiness.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_043_AUTH_POLICY_READINESS.md`

## Program.cs additions

```csharp
using Migration.ControlPlane.Auth;

builder.Services.AddAuthPolicyReadiness();
api.MapAuthPolicyReadinessEndpoints();
```

## New API route

```http
GET /api/cloud/auth/policy-readiness
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-auth-policy-readiness.ps1 -BaseUrl http://localhost:5173
```
