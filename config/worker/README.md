# Queue Executor Worker Configuration Templates

## Purpose

These templates document safe queue executor worker configuration modes.

They do not enable live execution by default.

## Templates

| File | Purpose |
|---|---|
| `queue-executor.dryrun.appsettings.example.json` | Default Azure Queue-shaped dry-run config |
| `queue-executor.local-inmemory.appsettings.example.json` | Local no-persistence in-memory mode |
| `queue-executor.azurequeue.appsettings.example.json` | Azure Queue managed identity mode |

## Safety defaults

All templates keep:

```json
{
  "QueueWorkerLoop": {
    "Enabled": false,
    "DryRun": true,
    "CompleteMessages": false
  },
  "QueueExecutorCoordinator": {
    "DryRun": true,
    "CompleteMessages": false
  }
}
```

## When live execution is allowed later

Only enable live execution when:

- queue receive provider is configured
- artifact storage is configured
- failure artifact handling is validated
- idempotency/lease enforcement exists
- message completion policy is approved
