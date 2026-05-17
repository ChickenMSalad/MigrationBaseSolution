# Release Manifest

## Purpose

A release manifest records what was built and what cloud diagnostics/deployment references apply to that release.

## Script

```powershell
tools/release/new-release-manifest.ps1
```

## Example

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\release\new-release-manifest.ps1 `
  -Version 0.1.0-dev `
  -EnvironmentName dev
```

## Output

```text
artifacts/release/release-manifest.json
```

## Includes

- version
- environment
- git branch/commit
- publish artifact paths
- diagnostic endpoint references
- deployment script references
- promotion checklist reference
