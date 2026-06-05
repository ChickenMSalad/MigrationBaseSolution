# P2 Set 009 — Azure Blob Provider Validation

## Purpose

P2 Set 009 adds a safe diagnostics endpoint and smoke test for the Azure Blob provider scaffold.

This does not switch local development to Azure Blob.

## Added files

- `src/Migration.Admin.Api/Endpoints/AzureBlobStorageDiagnosticsEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/azureBlobStorageDiagnostics.ts`
- `tools/test/smoke-azure-blob-storage-provider.ps1`
- `tools/test/smoke-azure-blob-storage-provider.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_009_AZURE_BLOB_PROVIDER_VALIDATION.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
api.MapAzureBlobStorageDiagnosticsEndpoints();
```

## New API route

```http
GET /api/cloud/storage/azure-blob/diagnostics
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Start Admin API, then:

```powershell
Invoke-RestMethod http://localhost:5173/api/cloud/storage/azure-blob/diagnostics
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-azure-blob-storage-provider.ps1 -BaseUrl http://localhost:5173
```

For actual Azure Blob config later:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-azure-blob-storage-provider.ps1 -BaseUrl http://localhost:5173 -ExpectAzureBlob
```
