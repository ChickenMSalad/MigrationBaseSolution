# P2 Set 003 — Local Binary Storage Provider

## Purpose

P2 Set 003 adds a real local file-system implementation of the binary storage provider contract introduced in P2 Set 002.

This still does not switch existing project/artifact persistence over to the new provider.

## Added files

- `src/Migration.ControlPlane/Storage/LocalFileSystemCloudBinaryStorageProvider.cs`
- `src/Migration.Admin.Api/Endpoints/CloudBinaryStorageProbeEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/cloudBinaryStorage.ts`
- `docs/cloud-roadmap-cleanup/P2_SET_003_LOCAL_BINARY_STORAGE_PROVIDER.md`

## Replaced file

- `src/Migration.ControlPlane/Storage/CloudBinaryStorageRegistrationExtensions.cs`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Changes expected:

```csharp
builder.Services.AddCloudBinaryStorage(builder.Configuration);
api.MapCloudBinaryStorageProbeEndpoints();
```

## New API routes

```http
GET /api/cloud/storage/provider
POST /api/cloud/storage/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then start Admin API:

```powershell
Invoke-RestMethod http://localhost:5173/api/cloud/storage/provider
Invoke-RestMethod -Method Post http://localhost:5173/api/cloud/storage/probe
```

The probe should create:

```text
.migration-control-plane/workspaces/default/artifacts/probe/storage-probe.txt
```
