# P2 Test Tool Inventory Report

Generated: 2026-05-18T09:17:08.5557715-04:00

## Summary

| Category | Count |
|---|---:|
| Core validators | 2 |
| Checkpoint validators | 5 |
| Endpoint smoke tests | 38 |
| CMD wrappers | 44 |
| Review candidates | 1 |

## Missing core validators
- None

## Missing checkpoint validators
- None

## Core validators
- validate-full-p2-stack.ps1
- validate-p2-completion.ps1

## Checkpoint validators
- validate-audit-persistence-stack.ps1
- validate-auth-operations-stack.ps1
- validate-operational-diagnostics-stack.ps1
- validate-queue-execution-stack.ps1
- validate-telemetry-stack.ps1

## Endpoint smoke tests
- smoke-artifact-storage-bridge.ps1
- smoke-audit-artifact-persistence.ps1
- smoke-audit-event-writer.ps1
- smoke-audit-persistence.ps1
- smoke-auth-enforcement-diagnostics.ps1
- smoke-auth-policy-readiness.ps1
- smoke-azure-blob-storage-provider.ps1
- smoke-azure-queue-dispatch.ps1
- smoke-cloud-credential-provider.ps1
- smoke-cloud-operation-audit.ps1
- smoke-cloud-operation-telemetry.ps1
- smoke-credential-access-policy-readiness.ps1
- smoke-endpoint-policy-inventory.ps1
- smoke-operational-mode.ps1
- smoke-operational-readiness-rollups.ps1
- smoke-p2-readiness-report.ps1
- smoke-production-safety-gates.ps1
- smoke-queue-audit-events.ps1
- smoke-queue-contracts.ps1
- smoke-queue-dispatch.ps1
- smoke-queue-execution-governance.ps1
- smoke-queue-execution-observability.ps1
- smoke-queue-execution-planner.ps1
- smoke-queue-execution-readiness.ps1
- smoke-queue-executor-coordinator.ps1
- smoke-queue-failure-artifact.ps1
- smoke-queue-failure-handler.ps1
- smoke-queue-idempotency.ps1
- smoke-queue-poison-handling.ps1
- smoke-queue-receive.ps1
- smoke-queue-telemetry-events.ps1
- smoke-queue-worker-loop.ps1
- smoke-queue-worker-loop-diagnostics.ps1
- smoke-storage-provider-stack.ps1
- smoke-telemetry-event-writer.ps1
- smoke-telemetry-sink.ps1
- smoke-worker-bootstrap-templates.ps1
- smoke-worker-coordinator-registration-plan.ps1

## CMD wrappers
- smoke-artifact-storage-bridge.cmd
- smoke-audit-artifact-persistence.cmd
- smoke-audit-event-writer.cmd
- smoke-audit-persistence.cmd
- smoke-auth-enforcement-diagnostics.cmd
- smoke-auth-policy-readiness.cmd
- smoke-azure-blob-storage-provider.cmd
- smoke-azure-queue-dispatch.cmd
- smoke-cloud-credential-provider.cmd
- smoke-cloud-operation-audit.cmd
- smoke-cloud-operation-telemetry.cmd
- smoke-credential-access-policy-readiness.cmd
- smoke-endpoint-policy-inventory.cmd
- smoke-operational-mode.cmd
- smoke-operational-readiness-rollups.cmd
- smoke-p2-readiness-report.cmd
- smoke-production-safety-gates.cmd
- smoke-queue-audit-events.cmd
- smoke-queue-dispatch.cmd
- smoke-queue-execution-governance.cmd
- smoke-queue-execution-observability.cmd
- smoke-queue-execution-planner.cmd
- smoke-queue-execution-readiness.cmd
- smoke-queue-executor-coordinator.cmd
- smoke-queue-failure-artifact.cmd
- smoke-queue-failure-handler.cmd
- smoke-queue-idempotency.cmd
- smoke-queue-poison-handling.cmd
- smoke-queue-receive.cmd
- smoke-queue-telemetry-events.cmd
- smoke-queue-worker-loop.cmd
- smoke-queue-worker-loop-diagnostics.cmd
- smoke-storage-provider-stack.cmd
- smoke-telemetry-event-writer.cmd
- smoke-telemetry-sink.cmd
- smoke-worker-bootstrap-templates.cmd
- smoke-worker-coordinator-registration-plan.cmd
- validate-audit-persistence-stack.cmd
- validate-auth-operations-stack.cmd
- validate-full-p2-stack.cmd
- validate-operational-diagnostics-stack.cmd
- validate-p2-completion.cmd
- validate-queue-execution-stack.cmd
- validate-telemetry-stack.cmd

## Review candidates
- validate-worker-bootstrap-config.ps1

## Recommendation

- Keep all core validators.
- Keep checkpoint validators.
- Keep smoke tests while endpoints remain active.
- Review CMD wrappers later; they are convenience wrappers, not required by CI.
- Do not delete scripts from this report without running full P2 validation afterward.
