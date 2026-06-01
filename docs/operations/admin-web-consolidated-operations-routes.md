# Admin Web Consolidated Operations Routes

This document records the route consolidation for the canonical Admin Web.

- Canonical deployable UI: `src/Admin/Migration.Admin.Web`
- Feature-source only: `apps/migration-admin-ui`

The feature-source app is not a deployment target. Operational feature work should be migrated into the canonical Admin Web and exposed through its router and layout navigation.

P10.2T wires the migrated operations pages into the canonical shell so the UI is usable instead of accumulating orphan pages.
