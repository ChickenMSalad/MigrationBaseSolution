# Azure Storage Scaffold

## Purpose

This scaffold provisions storage resources independently from application hosting.

## Resources

- Storage account
- Artifact blob container
- Control-plane blob container
- Audit blob container
- Migration run Azure Queue

## What-if

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\storage\deploy-storage.ps1 `
  -ResourceGroupName migration-dev-rg `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -WhatIf
```

## Deploy

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\storage\deploy-storage.ps1 `
  -ResourceGroupName migration-dev-rg `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration
```

## Outputs needed later

- `storageAccountResourceId`
- `storageAccountName`
- `artifactContainerName`
- `controlPlaneContainerName`
- `auditContainerName`
- `runQueueName`
