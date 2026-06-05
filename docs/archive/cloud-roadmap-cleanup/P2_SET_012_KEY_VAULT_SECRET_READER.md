# P2 Set 012 — Key Vault Secret Reader

## Purpose

P2 Set 012 adds a real Key Vault-backed credential value provider behind an interface.

This does not migrate connector credential usage yet and does not expose secret values through API endpoints.

## Added files

- `src/Migration.ControlPlane/Credentials/ICloudCredentialValueProvider.cs`
- `src/Migration.ControlPlane/Credentials/NullCloudCredentialValueProvider.cs`
- `src/Migration.ControlPlane/Credentials/KeyVaultCloudCredentialValueProvider.cs`
- `src/Migration.ControlPlane/Credentials/CloudCredentialValueRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/CloudCredentialValueProbeEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/cloudCredentialValues.ts`
- `tools/test/smoke-cloud-credential-provider.ps1`
- `tools/test/smoke-cloud-credential-provider.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_012_KEY_VAULT_SECRET_READER.md`

## Modified files

- `src/Migration.ControlPlane/Migration.ControlPlane.csproj`
- `Directory.Packages.props`
- `src/Migration.Admin.Api/Program.cs`

Adds package:

```xml
Azure.Security.KeyVault.Secrets
```

## New API route

```http
GET /api/cloud/credentials/secret-exists
```

This endpoint returns only whether the named secret exists. It never returns secret values.

## Validation

```powershell
dotnet restore .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Start Admin API, then:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-cloud-credential-provider.ps1 -BaseUrl http://localhost:5173
```
