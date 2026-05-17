# Azure Deployment Orchestration

## Purpose

P1 Set 030 consolidates the Azure scaffold into one reviewable orchestration flow.

## Main script

```powershell
deploy/azure/deploy-cloud-scaffold.ps1
```

## What it can run

- Storage scaffold
- Key Vault scaffold
- Admin API App Service scaffold
- Queue Executor Container Apps Job scaffold

## What it intentionally does not run

- RBAC assignments
- app package deployment
- container image push
- Key Vault secret creation
- production promotion

Those remain separate because they depend on outputs, principal IDs, and approval gates.

## Example what-if

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\deploy-cloud-scaffold.ps1 `
  -ResourceGroupName migration-dev-rg `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -QueueExecutorImage <registry>/migration-queue-executor:dev `
  -WhatIf
```

## Example partial what-if

Skip worker while validating base infrastructure:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\deploy-cloud-scaffold.ps1 `
  -ResourceGroupName migration-dev-rg `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -SkipWorker `
  -WhatIf
```
