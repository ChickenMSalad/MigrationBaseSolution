# P3 Operational Run Timeline Dashboard Verification

## Purpose

Verify that the timeline dashboard aggregate endpoint is mapped and consistent with its component endpoints.

## Endpoint verified

```http
GET /api/operational/runs/{runId}/timeline/dashboard?previewLimit=5
```

## Component endpoints compared

```http
GET /api/operational/runs/{runId}/dashboard
GET /api/operational/runs/{runId}/timeline/metrics
GET /api/operational/runs/{runId}/timeline/query?limit=5
```

## Full verification

```powershell
./scripts/operational-run-timeline-dashboard-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
