# Deployment Outputs to App Settings

## Purpose

After Azure infrastructure is provisioned, deployment outputs need to become application configuration.

This document describes the mapping from Azure resource outputs to Admin API / worker app settings.

## Script

```powershell
tools/cloud/new-cloud-appsettings-from-outputs.ps1
```

## Example

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
  -AuthAuthority https://login.microsoftonline.com/<tenant-id>/v2.0 `
  -AuthAudience api://<admin-api-client-id>
```

## Generated file

By default:

```text
config/environments/{environment}.generated.appsettings.json
```

## Important

Generated files may contain environment-specific identifiers.

Review before committing.

Do not include secrets.
