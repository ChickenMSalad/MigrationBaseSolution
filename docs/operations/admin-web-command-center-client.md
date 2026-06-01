# Admin Web Command Center Client

The canonical deployable Admin UI is `src/Admin/Migration.Admin.Web`.

The previous `apps/migration-admin-ui` app remains feature-source only and must not receive new P10 Admin UI feature work.

This set migrates the command-center read client into canonical Admin Web using a simple additive page, API module, and type module.

## Operational value

The Command Center page gives operators a single overview of runtime health, active runs, failed work, pending retries, worker health, notification status, and recent operational events when the matching Admin API endpoints are available.

## Route status

This set intentionally does not mutate route files. Route wiring should be handled in a later small set or manually after inspecting current `App.tsx` and `Layout.tsx`.
