# Legacy Solution Analysis

## Shared patterns observed

### Host shape repeated in all three repos
- Console host
- Functions host
- Dependency injection setup
- per-run execution context/state object

### Reusable helper areas repeated
- Azure Blob wrapper/factory
- CSV/Excel helpers
- logging/report writing
- options/configuration sections
- target API wrappers

### Signs of the current pain
- very large migration service classes
- client or migration-specific rules embedded in service code
- console plugins controlling business logic
- local filesystem paths / environment-specific assumptions in services
- mixed responsibilities: discovery + mapping + upload + retry + reporting in one class

## Extraction map

### Move into Domain/Application
- canonical asset and job models
- pipeline contracts
- mapping/validation contracts
- result/checkpoint contracts

### Move into Infrastructure
- blob wrappers
- SQL/SQLite repositories
- CSV/Excel IO
- HTTP resiliency/retry
- file/report output

### Move into Connectors
- AEM client
- Aprimo auth/asset clients
- WebDam API client
- Sitecore/Content Hub asset services
- Bynder API/client services
- S3 access layer
- Azure Blob source/target access layer

### Convert into Profiles
- field mappings
- taxonomy/classification rules
- file naming rules
- value coercion rules
- client-specific required field sets

## Refactor priority

1. Eliminate giant migration service classes by carving out pipeline steps.
2. Move source-specific code behind `IAssetSourceConnector`.
3. Move target-specific code behind `IAssetTargetConnector`.
4. Convert config-heavy logic into JSON profiles.
5. Centralize runtime state in SQL.
