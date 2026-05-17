# CI Validation Scaffold

## Purpose

P1 Set 022 adds a GitHub Actions validation workflow for the stabilized platform surface.

The workflow validates:

- Admin API build
- Queue Executor build
- Admin Web frontend build
- cloud diagnostics templates/docs

## Added file

- `.github/workflows/migration-platform-validation.yml`

## No deployment

This workflow does not deploy anything.

It does not require Azure credentials or secrets.

## Jobs

### backend

Builds:

- `src/Migration.Admin.Api/Migration.Admin.Api.csproj`
- `src/Workers/Migration.Workers.QueueExecutor/Migration.Workers.QueueExecutor.csproj`

### frontend

Runs:

```powershell
npm ci
npm run build
```

from:

```text
src/Admin/Migration.Admin.Web
```

### cloud-diagnostics

Runs:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\cloud\validate-cloud-diagnostics.ps1 -SkipHttp -Strict
```

## Notes

This workflow intentionally uses `windows-latest` because the repo and existing PowerShell paths have been exercised primarily on Windows.

A later P1/P2 set can add Linux/container validation after build portability is confirmed.
