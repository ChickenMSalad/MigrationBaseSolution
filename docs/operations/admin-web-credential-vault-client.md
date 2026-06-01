# Admin Web Credential Vault Client

P10.2L moves credential-vault visibility into the canonical Admin Web app.

The operational rule remains:

```text
src/Admin/Migration.Admin.Web = canonical deployable UI
apps/migration-admin-ui = feature-source/reference only
```

The migrated page lets operators view credential vault summary data, inspect the connector credential catalog, and validate one credential reference through the Admin API.
