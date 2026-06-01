# Admin Web Notification Routing Client

P10.2Q adds the notification routing client/page to the canonical Admin Web app.

The source reference is `apps/migration-admin-ui`, which remains feature-source only. The canonical deployable UI is `src/Admin/Migration.Admin.Web`.

## Operator view

The new page presents:

- notification route summary counts
- configured route rows
- alert preview rows
- load/error state

## Route status

This set does not automatically patch `App.tsx` or `Layout.tsx`. Route wiring should be done separately after confirming the committed source shape.
