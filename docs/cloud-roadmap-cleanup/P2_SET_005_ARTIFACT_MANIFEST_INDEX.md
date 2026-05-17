# P2 Set 005 — Artifact Manifest Index

## Purpose

P2 Set 005 adds a workspace-scoped artifact manifest index on top of the artifact storage service.

This is additive and still does not replace existing artifact endpoints.

## Added files

- `src/Migration.ControlPlane/Storage/ArtifactManifestContracts.cs`
- `src/Migration.ControlPlane/Storage/IArtifactManifestIndexService.cs`
- `src/Migration.ControlPlane/Storage/ArtifactManifestIndexService.cs`
- `src/Migration.ControlPlane/Storage/ArtifactManifestIndexRegistrationExtensions.cs`
- `src/Migration.Admin.Api/Endpoints/ArtifactManifestIndexEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/artifactManifestIndex.ts`
- `docs/cloud-roadmap-cleanup/P2_SET_005_ARTIFACT_MANIFEST_INDEX.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
builder.Services.AddArtifactManifestIndex();
api.MapArtifactManifestIndexEndpoints();
```

## New API routes

```http
GET /api/cloud/artifacts/index
POST /api/cloud/artifacts/index/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Then:

```powershell
Invoke-RestMethod http://localhost:5173/api/cloud/artifacts/index
Invoke-RestMethod -Method Post http://localhost:5173/api/cloud/artifacts/index/probe
Invoke-RestMethod http://localhost:5173/api/cloud/artifacts/index
```

Expected local index file:

```text
.migration-control-plane/workspaces/default/artifacts/other/_index/artifact-manifest-index.json
```
