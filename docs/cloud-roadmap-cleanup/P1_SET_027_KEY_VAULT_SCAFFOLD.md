# P1 Set 027 — Azure Key Vault Scaffold

## Purpose

P1 Set 027 adds Azure Key Vault infrastructure scaffolding and secret naming guidance.

This does not create secrets and does not change runtime behavior.

## Added files

- `deploy/azure/key-vault/main.bicep`
- `deploy/azure/key-vault/deploy-key-vault.ps1`
- `deploy/azure/key-vault/README.md`
- `docs/azure/KEY_VAULT_SECRET_NAMING.md`
- `docs/cloud-roadmap-cleanup/P1_SET_027_KEY_VAULT_SCAFFOLD.md`

## Resources planned

- Azure Key Vault with RBAC authorization
- soft delete enabled
- optional purge protection

## Validation

If Azure CLI is installed and logged in:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\key-vault\deploy-key-vault.ps1 `
  -ResourceGroupName <resource-group> `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -WhatIf
```
