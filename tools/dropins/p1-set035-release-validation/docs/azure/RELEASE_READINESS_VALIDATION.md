# Release Readiness Validation

## Purpose

Release readiness validation checks that the expected cloud deployment and release assets exist before cutting a release candidate.

## Script

```powershell
tools/release/validate-release-readiness.ps1
```

## Typical flow

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build\publish-cloud-artifacts.ps1 -Clean
powershell -ExecutionPolicy Bypass -File .\tools\release\new-release-manifest.ps1 -Version 0.1.0-dev -EnvironmentName dev
powershell -ExecutionPolicy Bypass -File .\tools\release\validate-release-readiness.ps1 -RequirePublishArtifacts -Strict
```

## What it validates

- release manifest exists and is valid JSON
- publish script exists
- release manifest script exists
- cloud diagnostics validator exists
- Azure deployment scaffold files exist
- promotion checklist exists
- optional publish outputs exist
