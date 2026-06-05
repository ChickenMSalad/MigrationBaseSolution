# P1 Set 035 — Release Readiness Validation

## Purpose

P1 Set 035 adds release readiness validation tooling.

This does not deploy anything.

## Added files

- `tools/release/validate-release-readiness.ps1`
- `tools/release/validate-release-readiness.cmd`
- `docs/azure/RELEASE_READINESS_VALIDATION.md`
- `docs/cloud-roadmap-cleanup/P1_SET_035_RELEASE_READINESS_VALIDATION.md`

## Validation

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\release\validate-release-readiness.ps1
```

Strict validation with publish artifacts:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build\publish-cloud-artifacts.ps1 -Clean
powershell -ExecutionPolicy Bypass -File .\tools\release\new-release-manifest.ps1 -Version 0.1.0-dev -EnvironmentName dev
powershell -ExecutionPolicy Bypass -File .\tools\release\validate-release-readiness.ps1 -RequirePublishArtifacts -Strict
```
