# P2 Set 001 — Blob Storage Abstractions

## Purpose

P2 Set 001 starts implementation-heavy cloud work by adding storage location abstractions.

This does not change existing storage behavior yet.

## Added files

- `src/Migration.ControlPlane/Storage/CloudStorageLocation.cs`
- `src/Migration.ControlPlane/Storage/ICloudStoragePathResolver.cs`
- `src/Migration.ControlPlane/Storage/CloudStoragePathResolver.cs`
- `src/Migration.ControlPlane/Storage/CloudStorageRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/CloudStoragePlanEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/cloudStorageLocations.ts`
- `docs/cloud-roadmap-cleanup/P2_SET_001_BLOB_STORAGE_ABSTRACTIONS.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
builder.Services.AddCloudStoragePathResolution(builder.Configuration);
api.MapCloudStoragePlanEndpoints();
```

## New API route

```http
GET /api/cloud/storage/locations
```

Optional query:

```http
GET /api/cloud/storage/locations?projectId=my-project&runId=my-run
```

Optional header:

```http
X-Workspace-Id: workspace-dev
```

## Why this matters

Before switching real project/run/artifact storage to Azure Blob, the platform needs one common resolver for workspace-scoped paths.

Future P2 sets can plug this into:

- project store
- artifact upload/download
- run state store
- audit persistence
- worker checkpointing

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then:

```powershell
Invoke-RestMethod http://localhost:5173/api/cloud/storage/locations
```
