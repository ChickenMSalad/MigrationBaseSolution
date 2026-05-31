# Migration Admin UI Canonicalization Notice

This app is not the canonical deployable Admin UI.

Canonical Admin UI:

```text
src/Admin/Migration.Admin.Web
```

This folder is retained as a feature source/prototype while useful operational UI features are migrated into the canonical Admin Web application.

Do not add new P10 Admin UI feature work here unless the work is explicitly marked as prototype-only.

Canonicalization note: this feature-source app is located at apps/migration-admin-ui and must be migrated into src/Admin/Migration.Admin.Web before future Admin UI feature work continues.
