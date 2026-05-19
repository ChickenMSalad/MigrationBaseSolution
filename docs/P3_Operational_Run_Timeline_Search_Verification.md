# P3 Operational Run Timeline Search Verification

## Purpose

Verify that the operational run timeline search endpoint is mapped and returns consistent results.

## Endpoint verified

```http
GET /api/operational/runs/{runId}/timeline/search?q=&limit=
```

## Checks

- route is present in `/api/system/endpoints`
- search result event count matches returned array length
- search respects `limit`
- search events are a subset of the full timeline
- search events match the requested search text

## Run full verification

```powershell
./scripts/operational-run-timeline-search-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
