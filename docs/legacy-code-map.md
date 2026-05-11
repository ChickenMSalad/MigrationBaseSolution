# Legacy code map into the consolidated solution

## Moved into new projects

### Shared
- `legacy/webdam-bynder-s3-azure/Bynder.Common/AzureStorage/*`
  -> `src/Migration.Shared/AzureStorage/*`
- `legacy/webdam-bynder-s3-azure/Bynder.Common/FileHelpers/*`
  -> `src/Migration.Shared/FileHelpers/*`

### WebDam source
- `legacy/webdam-bynder-s3-azure/Bynder.Common/WebDam/*`
  -> `src/Migration.Connectors.Sources.WebDam/Imported/WebDam/*`

### S3 source
- `legacy/webdam-bynder-s3-azure/Bynder.Common/S3/*`
  -> `src/Migration.Connectors.Sources.S3/Imported/S3/*`

### Bynder target
- `legacy/webdam-bynder-s3-azure/Bynder.Common/Api/*`
  -> `src/Migration.Connectors.Targets.Bynder/Imported/Api/*`
- `legacy/webdam-bynder-s3-azure/Bynder.Common/Bynder/*`
  -> `src/Migration.Connectors.Targets.Bynder/Imported/Bynder/*`
- `legacy/webdam-bynder-s3-azure/Bynder.Common/Models/*`
  -> `src/Migration.Connectors.Targets.Bynder/Imported/Models/*`
- `legacy/webdam-bynder-s3-azure/Bynder.Common/Extensions/*`
  -> `src/Migration.Connectors.Targets.Bynder/Imported/Extensions/*`

### AEM source
- `legacy/ashley-aem-azure-aprimo/src/Ashley.Core/{Abstractions,Models,Options,Services,Helpers,Extensions,Converters,Configuration}/*`
  -> `src/Migration.Connectors.Sources.Aem/Imported/...`

### Aprimo target
- `legacy/ashley-aem-azure-aprimo/src/Ashley.Core/{Abstractions,Models,Options,Services,Helpers,Extensions,Converters,Configuration}/*`
  -> `src/Migration.Connectors.Targets.Aprimo/Imported/...`
- `legacy/ashley-aem-azure-aprimo/src/Ashley.Core/Rules/*`
  -> `src/Migration.Connectors.Targets.Aprimo/Imported/Rules/*`

### Sitecore / Content Hub source
- `legacy/crocs-sitecore-azure-bynder/Crocs.Common/{ContentHub,Node,Models}/*`
  -> `src/Migration.Connectors.Sources.Sitecore/Imported/...`

### SQL-backed manifest/state helpers
- `legacy/ashley-aem-azure-aprimo/src/Ashley.Core/SQL/*`
  -> `src/Migration.Manifest.Sql/Imported/SQL/*`
- `legacy/ashley-aem-azure-aprimo/src/Ashley.Core/SQLite/*`
  -> `src/Migration.Manifest.Sqlite/Imported/SQLite/*`

## No longer included as solution projects

The following old app shapes were intentionally removed from `MigrationBaseSolution.sln`:

- Ashley.Console
- Ashley.Core
- Ashley.Functions
- Bynder.Common
- Bynder.Console
- Bynder.Functions
- Crocs.Common
- Crocs.Console
- Crocs.Functions

The files can remain under `legacy/` for historical reference, but the solution now loads only the new consolidated platform.
