# Admin Web Runtime Run Detail Client

The canonical Admin Web now owns runtime dashboard client pages under `src/Admin/Migration.Admin.Web/src/pages`.

This page is intended to make the run-detail endpoint introduced earlier visible from the canonical site instead of using the feature-source `/apps/migration-admin-ui` application.

Operational validation flow:

1. Apply the route wiring script.
2. Run the P10.2H validator.
3. Rebuild `src/Admin/Migration.Admin.Web`.
4. Open `/runtime-dashboard`.
5. Navigate to `/runtime-dashboard/{runId}` for a run returned by the dashboard.
