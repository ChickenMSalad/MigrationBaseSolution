# P2 Set 006 — Artifact Storage Bridge Endpoints

## Purpose

P2 Set 006 adds bridge endpoints that exercise the new artifact storage service and manifest index for upload/download/delete.

This does not replace existing artifact endpoints yet.

## Added files

- `src/Migration.Admin.Api/Endpoints/ArtifactStorageBridgeEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/artifactStorageBridge.ts`
- `docs/cloud-roadmap-cleanup/P2_SET_006_ARTIFACT_STORAGE_BRIDGE.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
api.MapArtifactStorageBridgeEndpoints();
```

## New API routes

```http
POST   /api/cloud/artifacts/{artifactKind}/{artifactId}/files/{fileName}
GET    /api/cloud/artifacts/{artifactKind}/{artifactId}/files/{fileName}
DELETE /api/cloud/artifacts/{artifactKind}/{artifactId}/files/{fileName}
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Start Admin API, then:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5173/api/cloud/artifacts/manifest/test-bridge/files/test.txt `
  -ContentType "text/plain" `
  -Body "hello artifact bridge"

Invoke-WebRequest `
  -Uri http://localhost:5173/api/cloud/artifacts/manifest/test-bridge/files/test.txt `
  -OutFile .\artifact-bridge-test.txt

Invoke-RestMethod http://localhost:5173/api/cloud/artifacts/index

Invoke-RestMethod `
  -Method Delete `
  -Uri http://localhost:5173/api/cloud/artifacts/manifest/test-bridge/files/test.txt
```
