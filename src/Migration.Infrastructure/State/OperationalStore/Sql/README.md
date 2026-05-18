# SQL Operational Store Infrastructure

P3 Set 004 adds configuration binding for the SQL operational store only.

This set intentionally does not register repository implementations, does not change queue behavior, and does not change endpoint or worker execution flow.

## Configuration shape

```json
{
  "ConnectionStrings": {
    "MigrationOperationalStore": "Server=.;Database=MigrationBase;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "OperationalStore": {
    "Sql": {
      "ConnectionStringName": "MigrationOperationalStore",
      "SchemaName": "migration",
      "CommandTimeoutSeconds": 30
    }
  }
}
```

`SchemaName` defaults to `migration`, matching `Scripts/001_CreateOperationalStore.sql`.

`ConnectionString` is available for local/integration scenarios, but committed configuration should prefer `ConnectionStringName` plus the normal `ConnectionStrings` section.
