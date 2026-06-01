# Admin Web Execution Profiles Client

This page brings connector execution profile visibility into the canonical Admin Web.

- Canonical UI: `src/Admin/Migration.Admin.Web`
- feature-source UI: `apps/migration-admin-ui`

The page reads connector execution profile summary/catalog data and can validate a selected profile through the Admin API execution-profile endpoints.

Expected API endpoints:

- `/api/operational/connectors/execution-profiles/summary`
- `/api/operational/connectors/execution-profiles/catalog`
- `/api/operational/connectors/execution-profiles/validate`
