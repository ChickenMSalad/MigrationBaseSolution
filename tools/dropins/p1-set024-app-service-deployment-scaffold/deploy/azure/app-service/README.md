# Azure App Service Deployment Scaffold

## Purpose

This folder contains a first-pass Azure App Service infrastructure scaffold for the Admin API.

It does not deploy automatically.

## Files

- `main.bicep`
- `deploy-app-service.ps1`

## Prerequisites

- Azure CLI installed
- Azure CLI logged in
- target resource group created
- Bicep support available through Azure CLI

## What-if

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\app-service\deploy-app-service.ps1 `
  -ResourceGroupName <resource-group> `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -WhatIf
```

## Deploy

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\app-service\deploy-app-service.ps1 `
  -ResourceGroupName <resource-group> `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration
```

## Resources

- App Service Plan
- Admin API App Service
- Storage Account
- Artifact Blob Container
- Control Plane Blob Container
- Azure Queue

## Important gaps

This scaffold does not yet include:

- Key Vault
- role assignments
- private networking
- frontend deployment
- queue worker deployment
- GitHub Actions deployment
- production hardening
