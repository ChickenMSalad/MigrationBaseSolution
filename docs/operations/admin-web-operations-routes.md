# Admin Web Operations Routes

The canonical Admin Web application is `src/Admin/Migration.Admin.Web`.

P10.2K makes the operational pages migrated from the feature-source UI reachable from the canonical Admin Web shell.

## Canonical route map

| Route | Page |
| --- | --- |
| `/runtime-dashboard` | `RuntimeDashboard` |
| `/runtime-runs/:runId` | `RuntimeRunDetail` |
| `/execution-sessions` | `ExecutionSessions` |
| `/failure-retry` | `FailureRetry` |

`apps/migration-admin-ui` remains feature-source/reference only.
