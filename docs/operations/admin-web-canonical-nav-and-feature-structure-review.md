# Admin Web Canonical Navigation and Feature Structure Review

## Canonical UI

```text
src/Admin/Migration.Admin.Web
```

## Feature-source reference only

```text
apps/migration-admin-ui
```

## Review scope

This gate reviews canonical Admin Web navigation and feature placement. It does not mutate source files.

The report should be generated with:

```powershell
.\tools\runtime\New-P102AdminWebCanonicalNavAndFeatureStructureReport.ps1
```

Default output:

```text
artifacts/p10/admin-web-canonical-nav-and-feature-structure-review.md
```

## Follow-up rules

- Do not add new deployable Admin UI work under `apps/migration-admin-ui`.
- Do not delete feature-source files until all useful functionality is migrated and verified in `src/Admin/Migration.Admin.Web`.
- Move canonical Admin Web files by feature family, not by broad repo-wide scripts.
- Keep each migration set buildable and reviewable.
