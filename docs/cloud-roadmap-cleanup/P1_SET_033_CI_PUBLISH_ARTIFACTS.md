# P1 Set 033 — CI Publish Artifacts

## Purpose

P1 Set 033 updates the platform validation workflow so CI produces deployable build artifacts.

This does not deploy anything.

## Replaced file

- `.github/workflows/migration-platform-validation.yml`

## Added behavior

The workflow now uploads:

- `admin-api-publish`
- `queue-executor-publish`
- `publish-manifest`
- `admin-web-dist`

## Why this matters

This creates a bridge from validation-only CI to future release/deployment workflows.

Future deployment jobs can consume these artifacts instead of rebuilding inconsistently.

## Validation

No local build is required for this docs/workflow-only set.

Optional local check:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build\publish-cloud-artifacts.ps1 -Clean
```
