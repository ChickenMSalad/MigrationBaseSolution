# P3 Operational Failure Analytics API Surface Audit

## Purpose

This verification-only set audits the operational failure analytics API surface added through Set 124.

## Covered routes

```http
GET /api/operational/failures/recent
GET /api/operational/failures/metrics
GET /api/operational/failures/dashboard
GET /api/operational/failures/query
GET /api/operational/failures/catalog
GET /api/operational/failures/system-pair-metrics
GET /api/operational/failures/run-status-metrics
GET /api/operational/failures/analytics-dashboard
GET /api/operational/failures/filtered-analytics
GET /api/operational/failures/analytics-presets
GET /api/operational/failures/analytics-presets/{presetKey}
GET /api/operational/failures/analytics-presets/search
GET /api/operational/failures/analytics-preset-dashboard
GET /api/operational/failures/analytics-preset-favorites
GET /api/operational/failures/analytics-preset-favorites/{favoriteKey}
```

## Run full milestone smoke

```powershell
./scripts/operational-failure-analytics-milestone-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```

## Notes

This set does not add runtime APIs. It only adds verification scripts and documentation.
