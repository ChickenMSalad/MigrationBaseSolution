# Azure Managed Identity RBAC Scaffold

## Purpose

This scaffold defines the intended RBAC assignments for Admin API and Queue Executor managed identities.

It does not run unless you invoke the deployment script.

## Planned role assignments

| Principal | Scope | Role |
|---|---|---|
| Admin API identity | Storage account | Storage Blob Data Contributor |
| Admin API identity | Storage account | Storage Queue Data Contributor |
| Admin API identity | Key Vault | Key Vault Secrets User |
| Queue Executor identity | Storage account | Storage Blob Data Contributor |
| Queue Executor identity | Storage account | Storage Queue Data Contributor |
| Queue Executor identity | Key Vault | Key Vault Secrets User |

## What-if

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\rbac\deploy-managed-identity-rbac.ps1 `
  -ResourceGroupName <resource-group> `
  -AdminApiPrincipalId <admin-api-principal-id> `
  -QueueExecutorPrincipalId <queue-executor-principal-id> `
  -StorageAccountResourceId <storage-account-resource-id> `
  -KeyVaultResourceId <key-vault-resource-id> `
  -WhatIf
```

## Important

The Bicep file is a scaffold and should be reviewed before production use.
