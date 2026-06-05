# P1 Set 024 — Azure App Service Deployment Scaffold

## Purpose

P1 Set 024 adds the first Azure infrastructure scaffold for hosting the Admin API on Azure App Service.

This set does not deploy anything automatically.

## Added files

- `deploy/azure/app-service/main.bicep`
- `deploy/azure/app-service/deploy-app-service.ps1`
- `deploy/azure/app-service/README.md`
- `docs/azure/APP_SERVICE_DEPLOYMENT_SCAFFOLD.md`
- `docs/cloud-roadmap-cleanup/P1_SET_024_APP_SERVICE_DEPLOYMENT_SCAFFOLD.md`

## Resources planned

- App Service Plan
- Admin API App Service
- Storage Account
- Artifact Blob Container
- Control Plane Blob Container
- Azure Queue

## Validation

If Azure CLI is installed and logged in:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\app-service\deploy-app-service.ps1 `
  -ResourceGroupName <resource-group> `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -WhatIf
```
