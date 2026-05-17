# P2 Set 051 — Full P2 Validation Aggregator

## Purpose

P2 Set 051 adds a single validation entry point for the entire P2 stack.

This combines:

- operational diagnostics validation
- queue validation
- audit validation
- telemetry validation
- auth readiness validation
- production safety validation
- operational mode validation
- queue governance validation

## Added files

- `tools/test/validate-full-p2-stack.ps1`
- `tools/test/validate-full-p2-stack.cmd`
- `docs/cloud-roadmap-cleanup/P2_SET_051_FULL_VALIDATION_AGGREGATOR.md`

## Validation

Start Admin API, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\validate-full-p2-stack.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured
```

If artifact storage audit persistence is enabled:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\test\validate-full-p2-stack.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured -ExpectArtifactStorage
```
