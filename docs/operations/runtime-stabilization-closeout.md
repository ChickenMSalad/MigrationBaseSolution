# Runtime Stabilization Closeout

Use this document as the handoff checkpoint before moving into new runtime features.

## Required evidence

- Latest P7.9 closeout report passed.
- Latest P7.10 closeout report passed.
- Runtime NoOp smoke completed in Azure.
- Final Azure app settings export shows no stale runtime `MIGRATION_*` worker keys except explicitly retained telemetry keys.
- Runtime SQL FK validator passed against Azure SQL.

## Handoff rule

Do not start new runtime feature work if any of the above evidence is missing.
