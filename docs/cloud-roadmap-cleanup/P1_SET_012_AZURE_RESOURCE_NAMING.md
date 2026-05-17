# P1 Set 012 — Azure Resource Naming Conventions

## Purpose

P1 Set 012 establishes deterministic Azure resource naming conventions before real infrastructure automation begins.

This prevents drift between:

- local templates
- deployment scripts
- future Bicep/Terraform
- Key Vault naming
- storage naming
- queue naming
- workspace partitioning

## Added files

- `tools/cloud/generate-azure-resource-names.ps1`
- `tools/cloud/generate-azure-resource-names.cmd`
- `docs/azure/AZURE_RESOURCE_NAMING.md`
- `docs/cloud-roadmap-cleanup/P1_SET_012_AZURE_RESOURCE_NAMING.md`

## Naming goals

- deterministic
- environment-aware
- workspace-aware
- Azure-safe
- CI/CD-safe
- easy to grep/search

## Example patterns

| Resource | Pattern |
|---|---|
| Storage Account | `migrationdevsa` |
| Key Vault | `migration-dev-kv` |
| App Service | `migration-dev-admin-api` |
| Queue | `migration-runs-dev` |
| Artifact Container | `migration-artifacts-dev` |

## Validation

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\cloud\generate-azure-resource-names.ps1
```
