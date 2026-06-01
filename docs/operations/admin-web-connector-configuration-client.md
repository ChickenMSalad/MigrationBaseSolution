# Admin Web Connector Configuration Client

The connector configuration workspace is part of the Admin UI consolidation track.

The canonical implementation location is:

```text
src/Admin/Migration.Admin.Web
```

The feature-source reference location is:

```text
apps/migration-admin-ui
```

P10.2N adds only the canonical client/page files. It does not make `apps/migration-admin-ui` deployable and does not route users to that app.

## Operator value

The page lets operators:

- view connector configuration readiness summary;
- inspect source/target connector configuration catalog entries;
- draft connector setting values;
- validate connector configuration shape through the Admin API.

## Follow-up

A later route-wiring set should add the page to `src/Admin/Migration.Admin.Web/src/App.tsx` and `src/Admin/Migration.Admin.Web/src/components/Layout.tsx` once this page builds cleanly.
