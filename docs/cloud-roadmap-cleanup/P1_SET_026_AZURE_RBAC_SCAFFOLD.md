# P1 Set 026 — Azure Managed Identity RBAC Scaffold

## Purpose

P1 Set 026 adds Azure RBAC scaffolding for managed identities.

This does not grant permissions unless the deployment script is run.

## Added files

- `deploy/azure/rbac/managed-identity-rbac.bicep`
- `deploy/azure/rbac/deploy-managed-identity-rbac.ps1`
- `deploy/azure/rbac/README.md`
- `docs/azure/MANAGED_IDENTITY_RBAC_PLAN.md`
- `docs/cloud-roadmap-cleanup/P1_SET_026_AZURE_RBAC_SCAFFOLD.md`

## Validation

If Azure CLI is installed and logged in:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\rbac\deploy-managed-identity-rbac.ps1 `
  -ResourceGroupName <resource-group> `
  -AdminApiPrincipalId <admin-api-principal-id> `
  -QueueExecutorPrincipalId <queue-executor-principal-id> `
  -StorageAccountResourceId <storage-account-resource-id> `
  -KeyVaultResourceId <key-vault-resource-id> `
  -WhatIf
```
