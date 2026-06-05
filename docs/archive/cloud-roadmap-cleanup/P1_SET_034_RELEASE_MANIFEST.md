# P1 Set 034 — Release Manifest

## Purpose

P1 Set 034 adds release manifest generation.

This is release-readiness tooling only and does not deploy anything.

## Added files

- `tools/release/new-release-manifest.ps1`
- `tools/release/new-release-manifest.cmd`
- `docs/azure/RELEASE_MANIFEST.md`
- `docs/cloud-roadmap-cleanup/P1_SET_034_RELEASE_MANIFEST.md`

## Validation

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\release\new-release-manifest.ps1 -Version 0.1.0-dev -EnvironmentName dev
```
