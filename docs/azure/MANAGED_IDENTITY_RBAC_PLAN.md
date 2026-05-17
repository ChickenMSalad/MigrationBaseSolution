# Azure Managed Identity RBAC Plan

## Purpose

The platform should avoid connection strings in cloud environments and prefer managed identity.

## Minimum planned assignments

### Admin API

- Storage Blob Data Contributor
- Storage Queue Data Contributor
- Key Vault Secrets User

### Queue Executor

- Storage Blob Data Contributor
- Storage Queue Data Contributor
- Key Vault Secrets User

## Future hardening

Later iterations should narrow scopes from storage account to specific containers/queues where practical.
