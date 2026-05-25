# P9J Azure Resource Provisioning Inventory

GeneratedUtc: 2026-05-25T02:35:10.9966466+00:00

This inventory verifies repository-side Azure resource provisioning planning before actual cloud resource creation.

## docs\p9\P9J-Azure-Resource-Provisioning-Plan.md

Present.
- Contains: Required Azure resources
- Contains: Provision first. Deploy disabled second. Enable last.
- Contains: Do not configure a production RunId override.
- Contains: MIGRATION_ConnectionStrings__MigrationOperationalStore
- Contains: Success criteria

## config\templates\p9j-azure-resource-provisioning.template.json

Present.
- Contains: resourceGroupName
- Contains: MigrationOperationalStore
- Contains: serviceBus
- Contains: applicationInsightsName
- Contains: productionRunIdOverrideAllowed

## Next human actions

- Choose Azure subscription, resource group, and region.
- Provision Azure SQL and Service Bus.
- Configure Application Insights or Azure Monitor connection string.
- Deploy apps disabled first.
- Enable workers only after SQL and Service Bus validation.
