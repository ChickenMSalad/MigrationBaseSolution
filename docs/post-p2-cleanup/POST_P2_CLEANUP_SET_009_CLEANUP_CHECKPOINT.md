# Post-P2 Cleanup Set 009 — Cleanup Checkpoint

## Purpose

This set adds a single post-P2 cleanup checkpoint script.

It runs the maintenance audits added during cleanup and writes a final checkpoint report.

## Audits included

```text
tools/maintenance/audit-p2-docs-tools.ps1
tools/maintenance/audit-p2-test-tools.ps1
tools/maintenance/audit-p2-docs.ps1
tools/maintenance/audit-p2-source-structure.ps1
tools/maintenance/audit-p2-comment-coverage.ps1
```

## Output

```text
docs/post-p2-cleanup/POST_P2_CLEANUP_CHECKPOINT_REPORT.md
```

## Behavior

This is reporting only.

It does not:

- delete files
- modify source code
- run git rm
- change runtime behavior

## Usage

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\maintenance\validate-post-p2-cleanup.ps1
```

Recommended after this:

```powershell
dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj
powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured
```
