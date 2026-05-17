# Azure Resource Naming

## Standard Prefix

Default prefix:

```text
migration
```

## Environment Codes

| Environment | Code |
|---|---|
| local-dev | local |
| development | dev |
| test | test |
| production | prod |

## Resource Patterns

### Storage account

```text
{prefix}{environment}sa
```

Example:

```text
migrationdevsa
```

### Key Vault

```text
{prefix}-{environment}-kv
```

### Admin API App Service

```text
{prefix}-{environment}-admin-api
```

### Queue name

```text
{prefix}-runs-{environment}
```

### Artifact container

```text
{prefix}-artifacts-{environment}
```

## Workspace partitioning

Workspace partitioning happens inside storage/container roots:

```text
workspaces/{workspaceId}/artifacts
workspaces/{workspaceId}/runs
workspaces/{workspaceId}/projects
```

## Important

These are conventions only.

This set does not provision Azure resources.
