# P2 Set 010 — Blob Switch Templates

## Purpose

P2 Set 010 adds storage configuration templates and a stack-level smoke test for switching the new storage abstraction from local file-system mode to Azure Blob mode.

This does not change the default local behavior.

## Added files

- `config/storage/azure-blob.localtest.appsettings.example.json`
- `config/storage/azure-blob.managedidentity.appsettings.example.json`
- `config/storage/README.md`
- `tools/test/smoke-storage-provider-stack.ps1`
- `tools/test/smoke-storage-provider-stack.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_010_BLOB_SWITCH_TEMPLATES.md`

## Validation

With the Admin API running in normal local mode:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-storage-provider-stack.ps1 -BaseUrl http://localhost:5173
```

Later, when running with Azure Blob configuration:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\smoke-storage-provider-stack.ps1 -BaseUrl http://localhost:5173 -ExpectAzureBlob
```
