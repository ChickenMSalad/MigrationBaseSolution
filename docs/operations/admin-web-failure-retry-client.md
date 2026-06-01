# Admin Web Failure / Retry Client

The canonical Admin Web app now owns the failure/retry client surface under:

```text
src/Admin/Migration.Admin.Web
```

The feature-source UI under `apps/migration-admin-ui` remains reference-only while feature families are migrated into the canonical app.

This set adds a read-focused page and API client. Mutating retry actions should only be enabled after the Admin API retry contract is verified against the current repo and cloud runtime.
