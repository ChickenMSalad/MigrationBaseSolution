# Admin Web Feature Folder Structure

Canonical Admin Web path:

`src/Admin/Migration.Admin.Web`

Feature-source reference path:

`apps/migration-admin-ui`

P10.2Z establishes the desired canonical grouping under:

`src/Admin/Migration.Admin.Web/src/features`

This is a structure checkpoint, not a mass move. Existing migrated files currently live in `pages`, `api`, and `types`; future consolidation sets can move one feature group at a time into the matching feature folder.

## Recommended move order

1. `features/operations`
2. `features/connectors`
3. `features/security`
4. `features/governance`
5. `features/platform`

Each move should preserve imports, rebuild Admin Web, and keep `apps/migration-admin-ui` as feature-source only until the migration report shows no remaining useful components.
