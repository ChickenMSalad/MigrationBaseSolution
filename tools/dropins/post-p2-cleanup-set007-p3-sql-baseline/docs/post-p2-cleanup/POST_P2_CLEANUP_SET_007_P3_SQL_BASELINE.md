# Post-P2 Cleanup Set 007 — P3 SQL Operational Baseline

## Purpose

This set adds the pre-P3 SQL operational model baseline.

It documents the agreed architecture direction:

```text
SQL Server = durable operational truth
Blob Storage = binaries, sidecars, reports, exports
Queues = execution coordination
Workers = execution engines
Admin API = governance/control plane
```

This is documentation only.

## Added docs

- `docs/p3-planning/P3_SQL_OPERATIONAL_MODEL.md`
- `docs/p3-planning/P3_SQL_SCHEMA_STARTING_POINT.md`
- `docs/p3-planning/P3_EXECUTION_BOUNDARIES.md`

## Why this belongs before P3

P3 should not start with worker execution code until the durable operational model is explicit.

For migrations with 500k–1.5M+ rows, CSV and Excel cannot be the operational run system. They should remain import/export/review artifacts only.
