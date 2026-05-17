# P2 Set 011 — Key Vault Credential Contracts

## Purpose

P2 Set 011 adds the credential naming/provider contract layer needed before real Key Vault credential resolution is wired into connector credentials.

This does not fetch secrets yet.

## Added files

- `src/Migration.ControlPlane/Credentials/CloudCredentialContracts.cs`
- `src/Migration.ControlPlane/Credentials/ICloudCredentialNameResolver.cs`
- `src/Migration.ControlPlane/Credentials/CloudCredentialNameResolver.cs`
- `src/Migration.ControlPlane/Credentials/CloudCredentialRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/CloudCredentialDiagnosticsEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/cloudCredentials.ts`
- `docs/cloud-roadmap-cleanup/P2_SET_011_KEY_VAULT_CREDENTIAL_CONTRACTS.md`

## Modified file

- `src\Migration.Admin.Api\Program.cs`

Adds:

```csharp
builder.Services.AddCloudCredentialPlanning(builder.Configuration);
api.MapCloudCredentialDiagnosticsEndpoints();
```

## New API routes

```http
GET /api/cloud/credentials/provider
GET /api/cloud/credentials/secret-name
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then:

```powershell
Invoke-RestMethod http://localhost:5173/api/cloud/credentials/provider
Invoke-RestMethod "http://localhost:5173/api/cloud/credentials/secret-name?role=source&connector=aem&credentialSet=default&secretKind=password"
```
