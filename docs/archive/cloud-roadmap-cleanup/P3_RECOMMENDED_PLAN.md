# P3 Recommended Plan

## Goal

P3 should convert the P2 diagnostics/governance foundation into controlled production execution.

## Recommended P3 phases

### P3-A — Auth Enforcement Rollout

- attach policies to route groups
- preserve local development bypass behavior
- validate auth failures explicitly
- add production-mode enforcement switch
- add smoke tests for authorized/unauthorized access

### P3-B — Live Queue Worker Enablement

- wire worker loop to queue executor coordinator
- add explicit execution-enabled option
- require production safety gate approval
- support message completion only after successful run planning/execution
- enforce poison/failure artifact behavior

### P3-C — Durable Audit + Telemetry Integrations

- add durable query model for audit events
- integrate Application Insights or equivalent telemetry provider
- preserve in-memory/local providers for dev
- add correlation across API, worker, queue, audit, telemetry

### P3-D — Deployment Switch-over

- validate real Azure Blob, Queue, Key Vault, and identity config
- add environment-specific deployment profiles
- verify production safety gates under deployed config
- document rollback path

### P3-E — Operational UI

- surface P2 readiness report
- surface production safety gates
- surface queue governance
- surface audit/telemetry recent events
- surface worker mode and queue status

## P3 safety rule

No destructive execution path should be enabled until:

- auth enforcement is active in production-like mode
- audit persistence is durable
- telemetry provider is configured
- queue governance allows live execution
- message completion is explicitly enabled
- manual approval is recorded
