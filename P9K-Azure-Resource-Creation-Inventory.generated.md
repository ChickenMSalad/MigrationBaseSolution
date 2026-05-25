# P9K Azure Resource Creation Inventory

GeneratedUtc: 2026-05-25T02:39:52.9295520+00:00

This inventory verifies repository-side Azure resource creation planning before actual resource provisioning.

## docs\p9\P9K-Azure-Resource-Creation-Plan.md

Present.
- Contains: Required Azure resources
- Contains: Provision first. Deploy disabled second. Enable last.
- Contains: Do not configure a production RunId override.
- Contains: Success criteria

## docs\p9\P9K-Azure-Cli-Resource-Creation-Runbook.md

Present.
- Contains: az group create
- Contains: az servicebus namespace create
- Contains: az servicebus queue create
- Contains: az monitor app-insights component create

## config\templates\p9k-azure-resource-creation.template.json

Present.
- Contains: resourceGroupName
- Contains: MigrationOperationalStore
- Contains: serviceBus
- Contains: applicationInsightsName
- Contains: productionRunIdOverrideAllowed

## Next human actions

- Choose Azure subscription, resource group, and region.
- Review docs/p9/P9K-Azure-Cli-Resource-Creation-Runbook.md.
- Provision Azure resources manually with reviewed Azure CLI commands.
- Keep workers disabled until SQL and Service Bus validation pass.
