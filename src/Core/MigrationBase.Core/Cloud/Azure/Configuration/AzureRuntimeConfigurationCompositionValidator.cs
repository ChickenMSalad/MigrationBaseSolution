using Microsoft.Extensions.Configuration;

namespace MigrationBase.Core.Cloud.Azure.Configuration;

/// <summary>
/// Lightweight configuration-shape validator used by hosts and scripts before deeper runtime validation.
/// </summary>
public static class AzureRuntimeConfigurationCompositionValidator
{
    public static AzureRuntimeConfigurationValidationResult Validate(IConfiguration configuration, AzureRuntimeEnvironmentProfile profile)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        List<string> errors = new();
        List<string> warnings = new();

        IConfigurationSection runtime = configuration.GetSection(AzureRuntimeConfigurationSourceNames.CloudRuntimeSection);
        if (!runtime.Exists())
        {
            errors.Add("Missing required configuration section 'AzureRuntime'.");
            return new AzureRuntimeConfigurationValidationResult(errors, warnings);
        }

        string? environmentName = runtime.GetSection("Environment")["Name"];
        if (!string.IsNullOrWhiteSpace(environmentName) &&
            !string.Equals(environmentName, profile.EnvironmentName, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"AzureRuntime:Environment:Name '{environmentName}' does not match selected profile '{profile.EnvironmentName}'.");
        }

        if (profile.RequiresDurableSqlOperationalStore)
        {
            string? connectionName = runtime.GetSection("SqlOperationalStore")["ConnectionStringName"];
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                errors.Add("AzureRuntime:SqlOperationalStore:ConnectionStringName is required for this environment profile.");
            }
        }

        if (profile.RequiresManagedIdentity)
        {
            string? useManagedIdentity = runtime.GetSection("Identity")["UseManagedIdentity"];
            if (!bool.TryParse(useManagedIdentity, out bool enabled) || !enabled)
            {
                errors.Add("AzureRuntime:Identity:UseManagedIdentity must be true for this environment profile.");
            }
        }

        if (profile.RequiresTelemetry)
        {
            string? telemetryEnabled = runtime.GetSection("Telemetry")["Enabled"];
            if (!bool.TryParse(telemetryEnabled, out bool enabled) || !enabled)
            {
                errors.Add("AzureRuntime:Telemetry:Enabled must be true for this environment profile.");
            }
        }

        return new AzureRuntimeConfigurationValidationResult(errors, warnings);
    }
}
