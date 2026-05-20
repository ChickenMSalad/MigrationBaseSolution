# P3 Operational Global Activity Query Verification

## Purpose

Verify that the global operational activity query endpoint is mapped and returns consistent filtered results.

## Endpoint verified

```http
GET /api/operational/activity/query?runId=&eventType=&source=&q=&limit=
```

## Checks

- route exists in `/api/system/endpoints`
- response limit matches requested limit
- event count matches event array length
- returned events do not exceed requested limit
- unfiltered query events are within the recent activity feed window
- source filter returns only matching sources
- search filter returns only matching events

## Run full verification

```powershell
./scripts/operational-global-activity-query-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
