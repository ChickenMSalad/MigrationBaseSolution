# P3 SQL Operational Store Scripts

This folder contains SQL Server scripts for the durable operational store.

## Set 003

`001_CreateOperationalStore.sql` creates the initial P3 operational schema:

- `migration.MigrationRuns`
- `migration.MigrationManifestRecords`
- `migration.MigrationWorkItems`
- `migration.MigrationIdentifierMaps`
- `migration.MigrationFailures`
- `migration.MigrationCheckpoints`

This set intentionally does not add EF, Dapper, DI registration, endpoints, worker changes, or queue behavior changes.
