namespace MigrationBase.Core.Cloud.Azure.Governance;

public sealed class AzureMaintenanceModeEvaluator : IAzureMaintenanceModeEvaluator
{
    public AzureMaintenanceModeDecision EvaluateNewRunAdmission(
        string environmentName,
        string? tenantKey,
        IReadOnlyCollection<AzureMaintenanceModeDescriptor> maintenanceModes,
        IReadOnlyCollection<AzureOperationalFreezeDescriptor> freezes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
        maintenanceModes ??= Array.Empty<AzureMaintenanceModeDescriptor>();
        freezes ??= Array.Empty<AzureOperationalFreezeDescriptor>();

        var maintenanceBlock = maintenanceModes.FirstOrDefault(mode =>
            IsActiveForEnvironment(mode, environmentName) &&
            mode.BlocksNewRuns &&
            (mode.Scope == AzureOperationalFreezeScope.Environment ||
             MatchesScope(mode.Scope, mode.ScopeKey, AzureOperationalFreezeScope.Tenant, tenantKey)));

        if (maintenanceBlock is not null)
        {
            return AzureMaintenanceModeDecision.Block(
                "maintenance_mode_blocks_new_runs",
                "New migration runs are blocked by active maintenance mode.",
                maintenanceMode: maintenanceBlock);
        }

        var freezeBlock = freezes.FirstOrDefault(freeze =>
            IsActiveForEnvironment(freeze, environmentName) &&
            freeze.BlocksExecution &&
            (freeze.Scope == AzureOperationalFreezeScope.Environment ||
             MatchesScope(freeze.Scope, freeze.ScopeKey, AzureOperationalFreezeScope.Tenant, tenantKey)));

        if (freezeBlock is not null)
        {
            return AzureMaintenanceModeDecision.Block(
                "operational_freeze_blocks_execution",
                "New migration runs are blocked by an active operational freeze.",
                operationalFreeze: freezeBlock);
        }

        return AzureMaintenanceModeDecision.Allow(
            "new_run_admission_allowed",
            "No active maintenance mode or operational freeze blocks new migration runs.");
    }

    public AzureMaintenanceModeDecision EvaluateWorkItemAdmission(
        string environmentName,
        string? hostRole,
        string? queueName,
        IReadOnlyCollection<AzureMaintenanceModeDescriptor> maintenanceModes,
        IReadOnlyCollection<AzureOperationalFreezeDescriptor> freezes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
        maintenanceModes ??= Array.Empty<AzureMaintenanceModeDescriptor>();
        freezes ??= Array.Empty<AzureOperationalFreezeDescriptor>();

        var maintenanceBlock = maintenanceModes.FirstOrDefault(mode =>
            IsActiveForEnvironment(mode, environmentName) &&
            mode.BlocksNewWorkItems &&
            (mode.Scope == AzureOperationalFreezeScope.Environment ||
             MatchesScope(mode.Scope, mode.ScopeKey, AzureOperationalFreezeScope.HostRole, hostRole) ||
             MatchesScope(mode.Scope, mode.ScopeKey, AzureOperationalFreezeScope.Queue, queueName)));

        if (maintenanceBlock is not null)
        {
            return AzureMaintenanceModeDecision.Block(
                "maintenance_mode_blocks_work_items",
                "New work-item admission is blocked by active maintenance mode.",
                maintenanceMode: maintenanceBlock);
        }

        var freezeBlock = freezes.FirstOrDefault(freeze =>
            IsActiveForEnvironment(freeze, environmentName) &&
            freeze.BlocksExecution &&
            (freeze.Scope == AzureOperationalFreezeScope.Environment ||
             MatchesScope(freeze.Scope, freeze.ScopeKey, AzureOperationalFreezeScope.HostRole, hostRole) ||
             MatchesScope(freeze.Scope, freeze.ScopeKey, AzureOperationalFreezeScope.Queue, queueName)));

        if (freezeBlock is not null)
        {
            return AzureMaintenanceModeDecision.Block(
                "operational_freeze_blocks_work_items",
                "New work-item admission is blocked by an active operational freeze.",
                operationalFreeze: freezeBlock);
        }

        return AzureMaintenanceModeDecision.Allow(
            "work_item_admission_allowed",
            "No active maintenance mode or operational freeze blocks work-item admission.");
    }

    public AzureMaintenanceModeDecision EvaluateDeploymentPromotion(
        string environmentName,
        IReadOnlyCollection<AzureOperationalFreezeDescriptor> freezes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
        freezes ??= Array.Empty<AzureOperationalFreezeDescriptor>();

        var freezeBlock = freezes.FirstOrDefault(freeze =>
            IsActiveForEnvironment(freeze, environmentName) &&
            (freeze.BlocksPromotion || freeze.BlocksDeployment) &&
            freeze.Scope == AzureOperationalFreezeScope.Environment);

        if (freezeBlock is not null)
        {
            return AzureMaintenanceModeDecision.Block(
                "operational_freeze_blocks_promotion",
                "Deployment promotion is blocked by an active operational freeze.",
                operationalFreeze: freezeBlock);
        }

        return AzureMaintenanceModeDecision.Allow(
            "deployment_promotion_allowed",
            "No active operational freeze blocks deployment promotion.");
    }

    private static bool IsActiveForEnvironment(AzureMaintenanceModeDescriptor mode, string environmentName)
    {
        if (!StringComparer.OrdinalIgnoreCase.Equals(mode.EnvironmentName, environmentName))
        {
            return false;
        }

        if (mode.State is AzureMaintenanceModeState.Disabled or AzureMaintenanceModeState.Expired)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (mode.StartsAtUtc is not null && mode.StartsAtUtc.Value > now)
        {
            return false;
        }

        if (mode.ExpiresAtUtc is not null && mode.ExpiresAtUtc.Value <= now)
        {
            return false;
        }

        return mode.State is AzureMaintenanceModeState.Enabled or AzureMaintenanceModeState.Scheduled;
    }

    private static bool IsActiveForEnvironment(AzureOperationalFreezeDescriptor freeze, string environmentName)
    {
        if (!StringComparer.OrdinalIgnoreCase.Equals(freeze.EnvironmentName, environmentName))
        {
            return false;
        }

        return freeze.ExpiresAtUtc is null || freeze.ExpiresAtUtc.Value > DateTimeOffset.UtcNow;
    }

    private static bool MatchesScope(
        AzureOperationalFreezeScope actualScope,
        string actualScopeKey,
        AzureOperationalFreezeScope expectedScope,
        string? expectedScopeKey) =>
        actualScope == expectedScope &&
        !string.IsNullOrWhiteSpace(expectedScopeKey) &&
        StringComparer.OrdinalIgnoreCase.Equals(actualScopeKey, expectedScopeKey);
}
