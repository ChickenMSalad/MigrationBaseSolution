# Admin Web Execution Sessions Client

This document records the first additive migration of the execution-session client surface into the canonical Admin Web project.

The feature-source implementation remains in `apps/migration-admin-ui/src/features/executionSessions`, but new deployable Admin UI work must target `src/Admin/Migration.Admin.Web`.

The added page reads recent execution sessions through the Admin API endpoint family used by the feature-source app:

- `GET /api/operational/execution-sessions/recent?take={take}`
- `POST /api/operational/execution-sessions`
- `POST /api/operational/events/snapshot`

Route wiring is intentionally deferred to the next set to keep this package low risk.
