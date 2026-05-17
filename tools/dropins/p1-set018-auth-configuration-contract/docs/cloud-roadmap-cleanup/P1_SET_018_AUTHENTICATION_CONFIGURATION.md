# P1 Set 018 — Authentication Configuration Contract

## Purpose

P1 Set 018 adds a safe authentication configuration contract.

This is planning/diagnostic only. It does not enable authentication or enforce authorization.

## Added files

- `src/Migration.Admin.Api/Contracts/AuthenticationConfigurationContracts.cs`
- `src/Migration.Admin.Api/Endpoints/AuthenticationConfigurationEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/authenticationConfiguration.ts`
- `docs/cloud-roadmap-cleanup/P1_SET_018_AUTHENTICATION_CONFIGURATION.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Only adds:

```csharp
api.MapAuthenticationConfigurationEndpoints();
```

## New API route

```http
GET /api/cloud/auth/configuration
```

## Safe output

The endpoint reports whether key auth settings are configured but does not expose actual authority, audience, client id, tenant id, secrets, or tokens.

## Validation

Run:

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then start the Admin API and call:

```powershell
Invoke-RestMethod http://localhost:5173/api/cloud/auth/configuration
```
