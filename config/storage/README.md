# Storage Configuration Templates

## Purpose

These templates show how to intentionally switch the new binary/artifact storage provider from local file-system mode to Azure Blob mode.

## Local default

Current development mode remains local:

```json
{
  "ControlPlane": {
    "StorageRoot": ".migration-control-plane"
  }
}
```

## Azure Blob selection rule

The Azure Blob provider is selected when:

```text
ControlPlane:StorageRoot
```

starts with:

```text
az://
```

or:

```text
https://
```

## Local/Azurite test

Use:

```text
config/storage/azure-blob.localtest.appsettings.example.json
```

## Managed identity/cloud test

Use:

```text
config/storage/azure-blob.managedidentity.appsettings.example.json
```

## Important

These are examples only. Do not commit real connection strings.
