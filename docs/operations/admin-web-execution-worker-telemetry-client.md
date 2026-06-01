# Admin Web Execution Worker Telemetry Client

This page brings execution worker heartbeat/telemetry visibility into the canonical Admin Web application.

## Source and destination

| Role | Path |
| --- | --- |
| Canonical UI | `src/Admin/Migration.Admin.Web` |
| Feature source/reference | `apps/migration-admin-ui/src/features/executionWorkers` |

## Endpoint

The canonical client uses the shared Admin Web API helper and reads:

```text
/api/operational/execution-workers/summary
```

with `staleAfterSeconds` as a query-string parameter.

## Follow-up

A later route-wiring set should expose the page in `App.tsx` and `Layout.tsx` after confirming the current committed file shape.

Consolidation note: apps/migration-admin-ui remains the feature-source only. The canonical deployable UI is src/Admin/Migration.Admin.Web.
