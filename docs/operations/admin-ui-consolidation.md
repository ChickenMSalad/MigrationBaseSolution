# Admin UI Consolidation

## Canonical UI

Use this as the deployable Admin UI:

```text
src/Admin/Migration.Admin.Web
```

## Prototype / feature source

Use this only as a source for controlled migration into the canonical UI:

```text
apps/migration-admin-ui
```

## Migration approach

Do not merge both apps wholesale. Move feature families one at a time, keeping the canonical app buildable after each set.

## P10 continuation point

P10.2D completed dashboard client work in the canonical Admin Web. P10.2E adds the consolidation map. After P10.2E, continue with feature-family migrations from `apps/migration-admin-ui` into `src/Admin/Migration.Admin.Web`.
