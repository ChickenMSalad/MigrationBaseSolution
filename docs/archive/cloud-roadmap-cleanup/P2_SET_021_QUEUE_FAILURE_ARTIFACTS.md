# P2 Set 021 — Queue Failure Artifacts

## Purpose

P2 Set 021 adds queue failure artifact contracts and probe endpoints.

This ties queue poison-handling planning to the artifact storage layer, but still does not change worker behavior.

## Added files

- `src/Migration.ControlPlane/Queues/QueueFailureArtifactContracts.cs`
- `src/Migration.ControlPlane/Queues/QueueFailureArtifactPlanner.cs`
- `src/Migration.Admin.Api/Endpoints/QueueFailureArtifactEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/queueFailureArtifacts.ts`
- `tools/test/smoke-queue-failure-artifact.ps1`
- `tools/test/smoke-queue-failure-artifact.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_021_QUEUE_FAILURE_ARTIFACTS.md`

## Modified file

- `src/Migration.Admin.Api/Program.cs`

Adds:

```csharp
api.MapQueueFailureArtifactEndpoints();
```

## New API routes

```http
GET /api/cloud/queue/failure-artifact/plan
POST /api/cloud/queue/failure-artifact/probe
```

## Validation

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
```

Start Admin API:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-queue-failure-artifact.ps1 -BaseUrl http://localhost:5173
```
