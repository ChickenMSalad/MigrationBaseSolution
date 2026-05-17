# P1 Set 030 — Azure Deployment Orchestration

## Purpose

P1 Set 030 adds a single orchestration entry point for the Azure deployment scaffolds created in prior sets.

This is still scaffold-only. It does not force deployment, create secrets, assign RBAC, push containers, or deploy app binaries.

## Added files

- `deploy/azure/deploy-cloud-scaffold.ps1`
- `deploy/azure/deploy-cloud-scaffold.cmd`
- `deploy/azure/README.md`
- `docs/azure/AZURE_DEPLOYMENT_ORCHESTRATION.md`
- `docs/cloud-roadmap-cleanup/P1_SET_030_AZURE_DEPLOYMENT_ORCHESTRATION.md`

## Validation

If Azure CLI is installed and logged in:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\deploy-cloud-scaffold.ps1 `
  -ResourceGroupName migration-dev-rg `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -QueueExecutorImage <registry>/migration-queue-executor:dev `
  -WhatIf
```
