# P9D - SQL Operational Store Cloud Validation

Purpose: validate that the Azure SQL operational store is ready before deploying workers or running cloud migration execution.

This set does not mutate schema. It validates readiness expectations and gives safe SQL commands to inspect the operational store.

## Scope

P9D verifies:

- Connection string configuration is present for `MigrationOperationalStore`.
- SQL operational schema files still exist in the repo.
- Runtime compatibility/bootstrap SQL scripts are present.
- Operational tables expected by the runtime can be inspected in Azure SQL.
- The cloud database can be checked without seeding or running migrations.

## Recommended order

From repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Validate-P9DSqlOperationalStoreCloudValidation.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\Write-P9DSqlOperationalStoreCloudValidationInventory.ps1
dotnet build
```

Then use the SQL inspection commands in:

```text
scripts/sql/P9D-InspectOperationalStore.sql
```

Run that script against the target Azure SQL database or localdb clone of the cloud schema.

## Azure SQL connection setting

The runtime expects this logical connection string name:

```text
ConnectionStrings:MigrationOperationalStore
```

For Azure-hosted workers, set the equivalent environment variable:

```text
MIGRATION_ConnectionStrings__MigrationOperationalStore
```

Do not put RunId overrides into production cloud configuration. Workers should discover runnable runs from SQL.

## What to verify before P9E

Before moving to Service Bus topology validation, confirm:

- Azure SQL database exists.
- The runtime can connect using the configured identity/connection string.
- Operational schema exists.
- Work item tables use the current ID model:
  - RunId is uniqueidentifier / Guid.
  - WorkItemId is bigint / long.
  - ManifestRowId is bigint / long nullable where applicable.
- Compatibility scripts have been applied.
- No seed data is required for worker startup.

## Manual SQL inspection

Use `scripts/sql/P9D-InspectOperationalStore.sql` to dump tables, columns, constraints, indexes, and selected row counts.

This is intentionally read-only.
