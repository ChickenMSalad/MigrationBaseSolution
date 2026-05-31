# Admin UI Feature Migration Matrix

## Canonical direction

The operational UI surface must converge on one deployable site:

```text
src/Admin/Migration.Admin.Web
```

The app under:

```text
apps/migration-admin-ui
```

is a feature source and reference implementation. It must not receive new P10 feature work unless the explicit purpose of the set is to prepare migration into the canonical site.

## How to use this set

Run:

```powershell
.\tools\runtime\New-P102AdminUiFeatureMigrationMatrix.ps1
```

Review the generated Markdown and JSON under:

```text
artifacts/admin-ui-consolidation
```

Use the matrix to select the next bounded migration slice. Each subsequent feature migration set should name the source family and the canonical destination path.
