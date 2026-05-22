# P4.10 — Operational Runtime Readiness Gate

## Purpose

P4.10 adds a SQL-backed readiness gate for the cloud runtime built in P4.1 through P4.9.

This set verifies that the SQL operational store is reachable, the required operational tables exist, and individual runs have enough durable state to start, dispatch, or execute safely.

## Added areas

```text
src/Core/Migration.Application/Operational/Readiness
src/Core/Migration.Infrastructure.Sql/Operational/Readiness
src/Core/Migration.Admin.Api/Endpoints/Operational/SqlBackbone
src/Core/Migration.Admin.Api/Registration
```

## Endpoints

```text
GET /api/operational/sql-backbone/runtime/readiness
GET /api/operational/sql-backbone/runtime/runs/{runId}/readiness
```

## Notes

- SQL remains the source of truth.
- The readiness layer is read-only.
- The endpoint is intentionally separate from the existing analytics endpoint families.
- No inline package versions are added.
- No broad project movement or solution restructuring is performed.
