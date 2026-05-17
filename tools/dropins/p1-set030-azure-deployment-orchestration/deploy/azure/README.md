# Azure Deployment Scaffold

## Purpose

This folder contains scaffolded Azure deployment assets for MigrationBaseSolution.

It is intentionally staged and reviewable.

## Current scaffold areas

| Area | Folder |
|---|---|
| Storage | `deploy/azure/storage` |
| Key Vault | `deploy/azure/key-vault` |
| Admin API App Service | `deploy/azure/app-service` |
| Queue Executor Container Apps Job | `deploy/azure/container-apps-job` |
| Managed Identity RBAC | `deploy/azure/rbac` |

## Orchestration script

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\deploy-cloud-scaffold.ps1 `
  -ResourceGroupName migration-dev-rg `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -QueueExecutorImage <registry>/migration-queue-executor:dev `
  -WhatIf
```

## Recommended order

1. Storage
2. Key Vault
3. Admin API App Service
4. Queue Executor job
5. Generate appsettings from outputs
6. Managed Identity RBAC
7. App package/container deployment
8. Health/readiness validation

## Notes

- The orchestration script can run `-WhatIf`.
- RBAC is intentionally separate because it needs principal IDs from created identities.
- The script does not deploy application binaries.
- The script does not create secrets.
