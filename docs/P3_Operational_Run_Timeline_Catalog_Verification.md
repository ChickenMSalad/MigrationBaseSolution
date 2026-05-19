# P3 Operational Run Timeline Catalog Verification

## Purpose

Verify that timeline catalog values match the timeline metrics source used to produce them.

## Endpoint verified

```http
GET /api/operational/runs/{runId}/timeline/catalog
```

## Component endpoint compared

```http
GET /api/operational/runs/{runId}/timeline/metrics
```

## Checks

- route exists in `/api/system/endpoints`
- catalog run id matches selected run id
- event type count matches event type array length
- source count matches source array length
- catalog event types match metrics event types
- catalog sources match metrics sources

## Run full verification

```powershell
./scripts/operational-run-timeline-catalog-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
