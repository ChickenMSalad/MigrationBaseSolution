# Post-P2 Cleanup Set 003 — Test Tool Inventory

## Purpose

This set adds a maintenance script that inventories `tools/test` after P2.

It classifies scripts into:

- core aggregate validators
- checkpoint validators
- endpoint smoke tests
- command wrappers
- potential review candidates

It does not delete anything.

## Why this cleanup exists

The P2 process created many endpoint smoke tests. Most are still useful, but the repo needs a clear picture of which scripts are part of the final validation harness and which are one-off or lower-level diagnostics.

## Usage

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\maintenance\audit-p2-test-tools.ps1
```

Outputs:

```text
docs/post-p2-cleanup/P2_TEST_TOOL_INVENTORY_REPORT.md
```

## Recommended policy

Keep:

```text
validate-p2-completion.ps1
validate-full-p2-stack.ps1
validate-operational-diagnostics-stack.ps1
validate-auth-operations-stack.ps1
validate-queue-execution-stack.ps1
validate-audit-persistence-stack.ps1
validate-telemetry-stack.ps1
```

Keep endpoint smoke tests while the API surface is still evolving.

Review later:

```text
duplicate command wrappers
old corrective smoke scripts
scripts for endpoints that no longer exist
```
