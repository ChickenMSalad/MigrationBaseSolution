# Unified Migration Platform Architecture

## Why this shape

Across the three legacy solutions, the recurring structure is:

- common/core library
- console host with plugin/menu pattern
- functions host for automation
- Azure Blob helper layer
- CSV/Excel helper layer
- large migration service classes that mix discovery, mapping, transfer, retries, logging, and reporting

The new base solution splits those concerns into stable layers.

## Proposed project layout

```text
src/
  Migration.Domain/
  Migration.Application/
  Migration.Infrastructure/
  Migration.Runner.Cli/
  Migration.Admin.Api/

  Migration.Manifest.Csv/
  Migration.Manifest.Excel/
  Migration.Manifest.Sql/
  Migration.Manifest.Sqlite/

  Migration.Connectors.Sources.Aem/
  Migration.Connectors.Sources.Sitecore/
  Migration.Connectors.Sources.WebDam/
  Migration.Connectors.Sources.AzureBlob/
  Migration.Connectors.Sources.S3/
  Migration.Connectors.Sources.SharePoint/

  Migration.Connectors.Targets.Bynder/
  Migration.Connectors.Targets.Aprimo/
  Migration.Connectors.Targets.AzureBlob/
  Migration.Connectors.Targets.Cloudinary/
```

## Core pipeline

1. Read job definition
2. Resolve source connector
3. Resolve target connector
4. Read manifest rows or discover source assets
5. Convert to canonical `AssetEnvelope`
6. Apply mapping profile
7. Apply transforms
8. Validate payload
9. Transfer binary + metadata
10. Persist checkpoint/result
11. Emit report/log events

## Canonical model

Every migration maps into a neutral model before going to the target.

- `AssetEnvelope`
- `AssetBinary`
- `MetadataBag`
- `TaxonomyAssignment`
- `RenditionRecord`
- `MigrationJobDefinition`
- `AssetWorkItem`
- `CheckpointRecord`
- `MigrationResult`

## What moves out of legacy monolith services

### AEM/Aprimo repo
Large service classes become:
- source adapter
- target adapter
- profile-driven mapper
- SQL checkpoint repository
- file/report service
- optional Aprimo-specific classification transformer

### WebDam/Bynder repo
Existing Bynder client and WebDam export logic become:
- `IAssetSourceConnector` for WebDam
- `IAssetTargetConnector` for Bynder
- reusable `BlobBinaryCache` / `AzureBlobBinaryStore`
- reusable metaproperty sync service as a target capability

### Sitecore/Bynder repo
Content Hub / Sitecore logic becomes:
- source adapter for Sitecore/Content Hub
- timer/event jobs become orchestration concerns
- reporting/update services become application-level post-processing services

## Cloud direction

Recommended steady-state Azure shape:

- API/UI: ASP.NET Core
- Orchestration: Durable Functions or queue-driven worker orchestration
- Background execution: Container App Job or worker service
- State: Azure SQL
- Secrets: Key Vault
- Work queue: Service Bus
- Blob cache/staging: Azure Blob Storage

## Migration strategy

### Sprint 1
Build the new scaffold and port shared helpers.

### Sprint 2
Port WebDam -> Bynder end-to-end.

### Sprint 3
Port AEM -> Azure -> Aprimo with SQL-backed state.

### Sprint 4
Port Sitecore -> Azure -> Bynder.

### Sprint 5
Add admin API and job submission UI.
