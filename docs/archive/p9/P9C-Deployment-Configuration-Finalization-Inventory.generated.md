# P9C Deployment Configuration Finalization Inventory

GeneratedUtc: 2026-05-25T01:53:48.4703237+00:00

This inventory is bounded. It avoids .git, bin, obj, .vs, node_modules, packages, and tools/dropins to prevent long-running scans.

## docs\p9\P9C-Deployment-Configuration-Finalization.md

Present.
- Contains: `ConnectionStrings:MigrationOperationalStore`
- Contains: `MIGRATION_OpenTelemetry__EnableTracing`
- Contains: `Do not configure a production RunId override`

## config\templates\p9c-deployment-configuration.template.json

Present.
- Contains: `MigrationOperationalStore`
- Contains: `SqlOperationalWorker`
- Contains: `ServiceBusDispatcher`
- Contains: `ServiceBusExecutor`
- Contains: `OpenTelemetry`

## src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs

Present.
- Contains: `AddOperationalOpenTelemetry`
- Contains: `AddEnvironmentVariables(prefix: "MIGRATION_")`

## src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs

Present.
- Contains: `AddOperationalOpenTelemetry`

## src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs

Present.
- Contains: `AddOperationalOpenTelemetry`

## Connection string references

No matches found.

## MIGRATION_ configuration provider references

- src\Connectors\Targets\Migration.Connectors.Targets.AzureBlob\Services\AzureBlobIntermediateStorageWriter.cs:48: metadata["migration_mapping_type"] = "intermediate";
- src\Connectors\Targets\Migration.Connectors.Targets.AzureBlob\AzureBlobTargetConnector.cs:151: ["migration_job"] = SafeBlobMetadataValue(job.JobName),
- src\Connectors\Targets\Migration.Connectors.Targets.AzureBlob\AzureBlobTargetConnector.cs:152: ["migration_work_item"] = SafeBlobMetadataValue(item.WorkItemId),
- src\Connectors\Targets\Migration.Connectors.Targets.AzureBlob\AzureBlobTargetConnector.cs:325: ["migration_job"] = SafeBlobMetadataValue(job.JobName),
- src\Connectors\Targets\Migration.Connectors.Targets.AzureBlob\AzureBlobTargetConnector.cs:326: ["migration_work_item"] = SafeBlobMetadataValue(item.WorkItemId),
- src\Connectors\Targets\Migration.Connectors.Targets.AzureBlob\AzureBlobTargetConnector.cs:361: AddBlobTagIfRoom(tags, "migration_work_item", item.WorkItemId);

## OpenTelemetry configuration references

No matches found.

## Service Bus configuration references

No matches found.
