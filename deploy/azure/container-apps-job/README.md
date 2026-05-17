# Azure Container Apps Job — Queue Executor Scaffold

## Purpose

This folder contains a first-pass Azure Container Apps Job scaffold for the Queue Executor worker.

It does not deploy automatically.

## What it creates

- Log Analytics workspace
- Container Apps managed environment
- Queue Executor Container Apps Job

## Why Container Apps Job?

The Queue Executor is a worker process. A scheduled job is a safe first cloud shape while lease/heartbeat and queue-triggered scaling are still evolving.

## What-if

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\azure\container-apps-job\deploy-container-apps-job.ps1 `
  -ResourceGroupName <resource-group> `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix migration `
  -QueueExecutorImage <registry>/migration-queue-executor:dev `
  -WhatIf
```

## Important gaps

Before production deployment, add:

- Azure Container Registry integration
- managed identity role assignments
- Key Vault access
- storage queue RBAC
- blob storage RBAC
- real image tags
- alerting
- retry/dead-letter behavior
