# Runtime Post-Clean Baseline Review

Use this checklist after P7.9/P7.10 cleanup has been applied.

## Required evidence

- Latest successful NoOp smoke RunId.
- SQL FK canonicalization validator output.
- Final dispatcher app settings snapshot.
- Final executor app settings snapshot.
- Runtime cleanup closeout gate report.
- Runtime deployment evidence bundle.

## Acceptance criteria

- Dispatcher starts without configuration errors.
- Executor starts without configuration errors.
- NoOp smoke reaches `RunCompleted` in executor logs.
- Work item reaches a terminal SQL state.
- Canonical runtime settings remain after restart.
- No runtime SQL/Service Bus `MIGRATION_*` duplicates remain, except explicitly retained telemetry keys.

## Do not regress

- Do not reintroduce `migration.MigrationRuns` as the parent for `migration.WorkItems`.
- Do not reintroduce fake Csv smoke into runtime smoke scripts.
- Do not deploy from dirty publish folders.
- Do not use appsetting cleanup scripts without reviewing generated deletes.
