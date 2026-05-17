# P1 Set 029 — Deployment Outputs to Configuration

## Purpose

P1 Set 029 adds tooling to turn Azure deployment outputs into appsettings-style configuration.

This does not deploy infrastructure and does not apply settings to Azure.

## Added files

- `tools/cloud/new-cloud-appsettings-from-outputs.ps1`
- `tools/cloud/new-cloud-appsettings-from-outputs.cmd`
- `docs/azure/DEPLOYMENT_OUTPUTS_TO_APPSETTINGS.md`
- `docs/cloud-roadmap-cleanup/P1_SET_029_DEPLOYMENT_OUTPUTS_CONFIG.md`

## Why this matters

The repo now has separate Azure scaffolds for:

- storage
- Key Vault
- App Service
- Queue Executor Container Apps Job
- RBAC

Once those produce outputs, the platform needs a repeatable way to convert those outputs into app settings.

## Validation

Example:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\cloud\new-cloud-appsettings-from-outputs.ps1 `
  -EnvironmentName dev `
  -WorkspaceId dev `
  -Region eastus `
  -StorageAccountName migrationdevsa `
  -ArtifactContainerName migration-artifacts-dev `
  -ControlPlaneContainerName migration-control-plane-dev `
  -RunQueueName migration-runs-dev `
  -KeyVaultUri https://migration-dev-kv.vault.azure.net/ `
  -AuthAuthority https://login.microsoftonline.com/example/v2.0 `
  -AuthAudience api://example
```
