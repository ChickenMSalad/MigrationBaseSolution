# P1 Set 028 — Azure Storage Scaffold

## Purpose

P1 Set 028 splits Azure storage infrastructure into its own scaffold.

This allows storage, queues, and containers to be reviewed/provisioned independently from App Service and worker hosting.

## Added files

- `deploy/azure/storage/main.bicep`
- `deploy/azure/storage/deploy-storage.ps1`
- `deploy/azure/storage/README.md`
- `docs/azure/STORAGE_INFRASTRUCTURE_PLAN.md`
- `docs/cloud-roadmap-cleanup/P1_SET_028_AZURE_STORAGE_SCAFFOLD.md`

## Validation

If Azure CLI is installed and logged in:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\storage\deploy-storage.ps1 `
  -ResourceGroupName migration-dev-rg `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -WhatIf
```
