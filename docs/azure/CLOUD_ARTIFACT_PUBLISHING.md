# Cloud Artifact Publishing

## Purpose

Before deployment automation pushes to Azure, the repo needs repeatable local/CI publish outputs.

## Script

```powershell
tools/build/publish-cloud-artifacts.ps1
```

## Usage

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build\publish-cloud-artifacts.ps1 -Clean
```

## Outputs

```text
artifacts/publish/admin-api
artifacts/publish/queue-executor
artifacts/publish/publish-manifest.json
```

## Later CI/CD usage

These outputs can feed:

- App Service zip deployment
- container image build
- artifact upload
- release packaging
