# P9K Azure Resource Creation Plan

This set moves from planning toward actual Azure provisioning, but it is intentionally safe: it does not run destructive commands and does not require secrets in source control.

## Goal

Create a repeatable Azure resource creation plan for the operational migration runtime.

## Required Azure resources

- Resource group
- Azure SQL logical server and database
- Azure Service Bus namespace and operational work-item queue
- Application Insights / Log Analytics linkage
- Container/App Service placeholders for the worker roles and Admin/API control plane

## Provisioning order

1. Choose subscription, region, and environment name.
2. Create or confirm resource group.
3. Create Azure SQL logical server and database.
4. Apply operational SQL schema and run inspection scripts.
5. Create Service Bus namespace and queue.
6. Create Application Insights / Azure Monitor resources.
7. Store connection strings securely as app settings or Key Vault references.
8. Deploy apps/workers disabled first.
9. Enable SQL worker, dispatcher, and executor only after validation.

## Deployment rule

Provision first. Deploy disabled second. Enable last.

## Production rules

- Do not configure a production RunId override.
- Use MIGRATION_ prefixed environment variables for cloud deployment settings.
- Keep SQL and Service Bus connection strings out of committed files.
- Prefer managed identity / Key Vault references where possible.
- Keep workers disabled until SQL, Service Bus, and telemetry checks are complete.

## Success criteria

- Azure SQL database exists and accepts schema inspection queries.
- Service Bus namespace and queue exist.
- Application Insights / Azure Monitor connection string is available.
- Deployment configuration can be produced without committing secrets.
- Worker roles have disabled-first settings ready.
