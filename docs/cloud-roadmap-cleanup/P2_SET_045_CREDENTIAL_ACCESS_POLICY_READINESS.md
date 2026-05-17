# P2 Set 045 — Credential Access Policy Readiness

## Purpose

P2 Set 045 adds credential access policy readiness diagnostics.

This is read-only and does not enforce auth yet.

## Added files

- `src/Migration.ControlPlane/Auth/CredentialAccessPolicyContracts.cs`
- `src/Migration.ControlPlane/Auth/ICredentialAccessPolicyReadinessService.cs`
- `src/Migration.ControlPlane/Auth/CredentialAccessPolicyReadinessService.cs`
- `src/Migration.ControlPlane/Auth/CredentialAccessPolicyReadinessRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/CredentialAccessPolicyReadinessEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/credentialAccessPolicyReadiness.ts`
- `tools/test/smoke-credential-access-policy-readiness.ps1`
- `tools/test/smoke-credential-access-policy-readiness.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_045_CREDENTIAL_ACCESS_POLICY_READINESS.md`

## Program.cs additions

```csharp
builder.Services.AddCredentialAccessPolicyReadiness();
api.MapCredentialAccessPolicyReadinessEndpoints();
```

## New API route

```http
GET /api/cloud/auth/credential-access-policy
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-credential-access-policy-readiness.ps1 -BaseUrl http://localhost:5173
```
