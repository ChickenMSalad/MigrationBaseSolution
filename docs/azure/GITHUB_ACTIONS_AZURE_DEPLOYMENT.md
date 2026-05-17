# GitHub Actions Azure Deployment Scaffold

## Purpose

P1 Set 031 adds a manual GitHub Actions workflow for Azure scaffold what-if/deploy.

It is intentionally manual and guarded.

## Workflow

```text
.github/workflows/azure-deployment-scaffold.yml
```

## Required GitHub secrets

Use OIDC/federated credentials:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
```

## Required GitHub environments

Create environments matching promotion targets:

- `dev`
- `test`
- `prod`

Recommended:

- require approval for `test`
- require approval for `prod`

## Behavior

The workflow always runs a what-if job first.

The deploy job only runs when:

```text
whatIfOnly = false
```

## Important

This workflow deploys only the scaffold infrastructure.

It does not:

- push container images
- deploy app binaries
- create Key Vault secrets
- apply RBAC automatically
- run production smoke tests
