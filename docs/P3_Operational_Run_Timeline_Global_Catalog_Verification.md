# P3 Operational Run Timeline Global Catalog Verification

## Purpose

Verify that the global timeline catalog endpoint is mapped and includes all values used by a selected run.

## Endpoint verified

```http
GET /api/operational/runs/timeline/catalog
```

## Component endpoint compared

```http
GET /api/operational/runs/{runId}/timeline/catalog
```

## Checks

- route exists in `/api/system/endpoints`
- global event type count matches event type array length
- global source count matches source array length
- global event types include selected run event types
- global sources include selected run sources

## Run full verification

```powershell
./scripts/operational-run-timeline-global-catalog-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
