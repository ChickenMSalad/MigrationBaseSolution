# P1 Set 032 — Cloud Artifact Publishing

## Purpose

P1 Set 032 adds a repeatable publish script for cloud deployment artifacts.

This does not deploy anything.

## Added files

- `tools/build/publish-cloud-artifacts.ps1`
- `tools/build/publish-cloud-artifacts.cmd`
- `docs/azure/CLOUD_ARTIFACT_PUBLISHING.md`
- `docs/cloud-roadmap-cleanup/P1_SET_032_CLOUD_ARTIFACT_PUBLISHING.md`

## Validation

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build\publish-cloud-artifacts.ps1 -Clean
```
