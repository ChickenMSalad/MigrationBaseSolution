# Azure Key Vault Scaffold

## Purpose

This scaffold creates a Key Vault configured for RBAC-based authorization.

It does not create secrets.

## What-if

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\key-vault\deploy-key-vault.ps1 `
  -ResourceGroupName <resource-group> `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -WhatIf
```

## Deploy

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\key-vault\deploy-key-vault.ps1 `
  -ResourceGroupName <resource-group> `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration
```

## Production note

For production, use:

```powershell
-EnablePurgeProtection
```

after confirming your recovery/deletion policy.
