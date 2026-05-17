# P1 Set 013 — Azure Environment Promotion Checklist

## Purpose

P1 Set 013 adds promotion checklists for moving from local-dev to dev/test/prod.

This is docs/tooling only and does not change runtime behavior.

## Added files

- `docs/azure/AZURE_ENVIRONMENT_PROMOTION_CHECKLIST.md`
- `tools/cloud/show-promotion-checklist.ps1`
- `tools/cloud/show-promotion-checklist.cmd`
- `docs/cloud-roadmap-cleanup/P1_SET_013_ENVIRONMENT_PROMOTION_CHECKLIST.md`

## Usage

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\cloud\show-promotion-checklist.ps1 -Environment dev
```

Allowed environments:

- `local-dev`
- `dev`
- `test`
- `prod`

## Why this matters

The repo now has cloud diagnostic endpoints and environment templates. This set defines the human promotion gates before we begin real infrastructure/deployment automation.
