# P3 Operational Global Activity Feed Verification

## Purpose

Verify that the global recent activity feed is mapped and internally consistent.

## Endpoint verified

```http
GET /api/operational/activity/recent?limit=10
```

## Checks

- route exists in `/api/system/endpoints`
- response limit matches requested limit
- event count matches event array length
- returned count does not exceed requested limit
- events are sorted descending by `occurredAt`
- each event has required fields
- each source belongs to the allowed operational tables

## Run full verification

```powershell
./scripts/operational-global-activity-feed-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```
