# Runtime Clean Handoff Index

## Clean handoff definition

The runtime is considered clean when:

- `migration.WorkItems` references canonical run parentage.
- Dispatcher and executor use canonical unprefixed app settings.
- RuntimeSmoke/NoOp smoke completes through dispatcher, Service Bus, executor, orchestration, and SQL state update.
- Legacy smoke samples are not used as readiness checks.
- Deployment evidence is captured in `artifacts` and linked from the handoff notes.

## Commands

Run the closeout validators first:

```powershell
.\tools\runtime\Invoke-RuntimeCleanupCloseoutGate.ps1 `
  -ConfigurationPath .\config-samples\runtime-cleanup-closeout-gate.sample.json `
  -OutputPath .\artifacts\runtime-cleanup-closeout
```

Then run the RuntimeSmoke/NoOp smoke with a fresh run id using the P7.9D scripts.

## Handoff warning

Do not treat a successful `dotnet build` as deployment readiness. Runtime readiness requires SQL contract validation, settings validation, deployment evidence, and NoOp smoke completion.
