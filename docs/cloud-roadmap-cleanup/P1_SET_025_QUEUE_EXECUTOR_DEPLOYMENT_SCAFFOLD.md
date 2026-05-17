# P1 Set 025 — Queue Executor Deployment Scaffold

## Purpose

P1 Set 025 adds Azure Container Apps Job deployment scaffolding for the Queue Executor worker.

This does not deploy anything automatically and does not change runtime behavior.

## Added files

- `deploy/azure/container-apps-job/main.bicep`
- `deploy/azure/container-apps-job/deploy-container-apps-job.ps1`
- `deploy/azure/container-apps-job/README.md`
- `docs/azure/QUEUE_EXECUTOR_CONTAINER_APPS_JOB.md`
- `docs/cloud-roadmap-cleanup/P1_SET_025_QUEUE_EXECUTOR_DEPLOYMENT_SCAFFOLD.md`

## Resources planned

- Log Analytics workspace
- Container Apps managed environment
- Queue Executor Container Apps Job

## Validation

If Azure CLI is installed and logged in:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\container-apps-job\deploy-container-apps-job.ps1 `
  -ResourceGroupName <resource-group> `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -QueueExecutorImage <registry>/migration-queue-executor:dev `
  -WhatIf
```
