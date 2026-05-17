# P2 Set 004 — Artifact Storage Contracts

## Purpose

P2 Set 004 adds typed artifact storage contracts and a service layer on top of the binary storage provider.

This still does not migrate existing artifact endpoints.

## Added files

- `src/Migration.ControlPlane/Storage/ArtifactStorageContracts.cs`
- `src/Migration.ControlPlane/Storage/IArtifactStorageService.cs`
- `src/Migration.ControlPlane/Storage/ArtifactStorageService.cs`
- `src/Migration.ControlPlane/Storage/ArtifactStorageRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/ArtifactStorageProbeEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/artifactStorage.ts`
- `docs/cloud-roadmap-cleanup/P2_SET_004_ARTIFACT_STORAGE_CONTRACTS.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
builder.Services.AddArtifactStorage();
api.MapArtifactStorageProbeEndpoints();
```

## New API routes

```http
GET /api/cloud/artifacts/resolve
POST /api/cloud/artifacts/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then:

```powershell
Invoke-RestMethod "http://localhost:5173/api/cloud/artifacts/resolve?kind=manifest&artifactId=test&fileName=test.json"
Invoke-RestMethod -Method Post http://localhost:5173/api/cloud/artifacts/probe
```
