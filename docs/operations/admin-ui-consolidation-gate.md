# Admin UI Consolidation Gate

## Canonical UI

`src/Admin/Migration.Admin.Web` is the deployable Admin UI.

## Feature-source UI

`apps/migration-admin-ui` is feature-source only. It should not receive new P10 Admin UI features.

## Recommended canonical grouping

Future UI cleanup should prefer feature folders in the canonical app:

```text
src/Admin/Migration.Admin.Web/src/features/runtimeDashboard
src/Admin/Migration.Admin.Web/src/features/executionSessions
src/Admin/Migration.Admin.Web/src/features/failures
src/Admin/Migration.Admin.Web/src/features/connectors
src/Admin/Migration.Admin.Web/src/features/credentials
src/Admin/Migration.Admin.Web/src/features/operations
src/Admin/Migration.Admin.Web/src/features/governance
```

Each feature folder should eventually own its page, API client, types, and local components.

## Current rule

Do not delete `apps/migration-admin-ui` yet. Use the consolidation report to identify feature families that still need migration or deprecation.
