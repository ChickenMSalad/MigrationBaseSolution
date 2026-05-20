# P3 Milestone 100 — Operational API Surface Audit

## Purpose

This is a milestone verification set. It audits the expected operational API surface and runs a compact health snapshot across the major P3 areas.

## Areas covered

- operational mirror diagnostics
- operational runs
- run control/finalization/reconciliation
- timeline APIs
- work-item leasing/recovery/expiration
- dispatcher diagnostics/history/retention/dashboard
- global activity feed/query/metrics/dashboard
- global failures
- operational metrics and diagnostics

## Run

```powershell
./scripts/operational-p3-milestone-100-full-smoke-test.ps1 -BaseUrl "https://localhost:55436"
```

## Note

This set is verification-only. It does not add runtime APIs or services.
