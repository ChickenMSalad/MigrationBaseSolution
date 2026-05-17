# P2 Set 008 — Azure Blob Provider Scaffold

## Purpose

P2 Set 008 adds the first real Azure Blob binary storage provider implementation behind the existing `ICloudBinaryStorageProvider` contract.

Local development remains local file-system based.

## Added files

- `src/Migration.ControlPlane/Storage/AzureBlobStorageOptions.cs`
- `src/Migration.ControlPlane/Storage/AzureBlobCloudBinaryStorageProvider.cs`
- `docs/cloud-roadmap-cleanup/P2_SET_008_AZURE_BLOB_PROVIDER_SCAFFOLD.md`

## Replaced file

- `src/Migration.ControlPlane/Storage/CloudBinaryStorageRegistrationExtensions.cs`

## Modified project file

- `src/Migration.ControlPlane/Migration.ControlPlane.csproj`

Adds package references:

```xml
<PackageReference Include="Azure.Identity" Version="1.13.2" />
<PackageReference Include="Azure.Storage.Blobs" Version="12.24.0" />
```

## Configuration

Azure Blob provider is selected only when:

```json
{
  "ControlPlane": {
    "StorageRoot": "az://migration-control-plane-dev"
  }
}
```

Provider can use either managed identity:

```json
{
  "AzureBlobStorage": {
    "AccountName": "migrationdevsa",
    "ContainerName": "migration-control-plane-dev",
    "UseManagedIdentity": true
  }
}
```

or connection string for local/integration testing:

```json
{
  "AzureBlobStorage": {
    "ConnectionString": "<connection-string>",
    "ContainerName": "migration-control-plane-dev"
  }
}
```

## Validation

Local build:

```powershell
dotnet restore .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
dotnet build .\src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj
```

Local behavior should remain local file-system unless `ControlPlane:StorageRoot` starts with `az://` or `https://`.
