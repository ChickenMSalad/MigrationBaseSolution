# P1 Set 031 — GitHub Actions Azure Deployment Scaffold

## Purpose

P1 Set 031 adds a manual GitHub Actions deployment scaffold for Azure what-if/deploy.

This is a CI/CD scaffold only. It will not run unless triggered manually and configured with Azure OIDC secrets.

## Added files

- `.github/workflows/azure-deployment-scaffold.yml`
- `docs/azure/GITHUB_ACTIONS_AZURE_DEPLOYMENT.md`
- `docs/cloud-roadmap-cleanup/P1_SET_031_GITHUB_ACTIONS_DEPLOYMENT_SCAFFOLD.md`

## Validation

No local build is required.

Recommended repo check:

```powershell
git status
```

Optional YAML review:

```powershell
Get-Content .\.github\workflows\azure-deployment-scaffold.yml
```

## Required secrets

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
