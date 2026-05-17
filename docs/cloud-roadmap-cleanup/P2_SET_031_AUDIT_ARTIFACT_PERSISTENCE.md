# P2 Set 031 — Audit Artifact Persistence

## Purpose

P2 Set 031 adds an artifact-backed audit persistence provider.

The default remains `InMemory` unless configured:

```json
{
  "Audit": {
    "Provider": "ArtifactStorage",
    "ArtifactKind": "audit",
    "ArtifactId": "events",
    "FileNamePrefix": "audit-event",
    "RecentQueryLimit": 100
  }
}
```

## Added files

- `src/Migration.ControlPlane/Audit/ArtifactAuditPersistenceOptions.cs`
- `src/Migration.ControlPlane/Audit/ArtifactAuditPersistenceProvider.cs`
- `src/Migration.Admin.Api/Endpoints/AuditArtifactPersistenceEndpointExtensions.cs`
- `src/Admin/Migration.Admin.Web/src/api/auditArtifactPersistence.ts`
- `tools/test/smoke-audit-artifact-persistence.ps1`
- `tools/test/smoke-audit-artifact-persistence.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_031_AUDIT_ARTIFACT_PERSISTENCE.md`

## Replaced file

- `src/Migration.ControlPlane/Audit/AuditPersistenceRegistrationExtensions.cs`

## Program.cs addition

```csharp
api.MapAuditArtifactPersistenceEndpoints();
```

## Validation

Default/in-memory mode:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-audit-artifact-persistence.ps1 -BaseUrl http://localhost:5173
```

Artifact mode:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-audit-artifact-persistence.ps1 -BaseUrl http://localhost:5173 -ExpectArtifactStorage
```
