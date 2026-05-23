namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition;

public sealed class AzureRuntimeCompositionPlanner : IAzureRuntimeCompositionPlanner
{
    public AzureRuntimeCompositionPlan CreatePlan(string environmentName, string hostRole)
    {
        var normalizedEnvironment = NormalizeRequired(environmentName, nameof(environmentName));
        var normalizedHostRole = NormalizeRequired(hostRole, nameof(hostRole));

        return new AzureRuntimeCompositionPlan
        {
            Name = $"{normalizedEnvironment}:{normalizedHostRole}:runtime-composition",
            EnvironmentName = normalizedEnvironment,
            HostRole = normalizedHostRole,
            RequiredConfigurationSections = new[]
            {
                "AzureRuntime",
                "AzureRuntime:Sql",
                "AzureRuntime:Storage",
                "AzureRuntime:Queueing",
                "AzureRuntime:Telemetry"
            },
            RequiredOperationalStores = new[]
            {
                "SqlOperationalStore",
                "ManifestStore",
                "ExecutionStateStore",
                "OperationalEventStore"
            },
            Steps = new[]
            {
                CreateStep("configuration", AzureRuntimeCompositionStepKind.Configuration, "Bind and validate environment-specific Azure runtime configuration."),
                CreateStep("options", AzureRuntimeCompositionStepKind.Options, "Materialize runtime options used by hosts and workers.", "configuration"),
                CreateStep("persistence", AzureRuntimeCompositionStepKind.Persistence, "Attach SQL-first operational stores for durable execution state.", "options"),
                CreateStep("queueing", AzureRuntimeCompositionStepKind.Queueing, "Attach queue dispatch and work-claim boundaries.", "persistence"),
                CreateStep("observability", AzureRuntimeCompositionStepKind.Observability, "Attach correlation, metrics, health, and alert signal surfaces.", "options"),
                CreateStep("governance", AzureRuntimeCompositionStepKind.Governance, "Attach maintenance, emergency stop, approval, and production readiness gates.", "persistence"),
                CreateStep("worker-runtime", AzureRuntimeCompositionStepKind.WorkerRuntime, "Attach execution worker lifecycle, heartbeat, retry, and poison-work policies.", "queueing", "observability", "governance"),
                CreateStep("validation", AzureRuntimeCompositionStepKind.Validation, "Validate the composed runtime before real migration execution.", "worker-runtime")
            }
        };
    }

    public AzureRuntimeCompositionValidationResult Validate(AzureRuntimeCompositionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(plan.Name))
        {
            errors.Add("Composition plan name is required.");
        }

        if (string.IsNullOrWhiteSpace(plan.EnvironmentName))
        {
            errors.Add("Composition plan environment name is required.");
        }

        if (string.IsNullOrWhiteSpace(plan.HostRole))
        {
            errors.Add("Composition plan host role is required.");
        }

        var steps = plan.Steps ?? Array.Empty<AzureRuntimeCompositionStep>();
        if (steps.Count == 0)
        {
            errors.Add("Composition plan must include at least one step.");
        }

        var stepNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.Name))
            {
                errors.Add("Composition step name is required.");
                continue;
            }

            if (!stepNames.Add(step.Name))
            {
                errors.Add($"Duplicate composition step detected: {step.Name}.");
            }

            foreach (var dependency in step.DependsOn ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(dependency))
                {
                    warnings.Add($"Composition step {step.Name} has an empty dependency entry.");
                }
            }
        }

        foreach (var step in steps)
        {
            foreach (var dependency in step.DependsOn ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(dependency) && !stepNames.Contains(dependency))
                {
                    errors.Add($"Composition step {step.Name} depends on missing step {dependency}.");
                }
            }
        }

        return errors.Count == 0
            ? AzureRuntimeCompositionValidationResult.Valid(warnings.ToArray())
            : AzureRuntimeCompositionValidationResult.Invalid(errors, warnings);
    }

    private static AzureRuntimeCompositionStep CreateStep(
        string name,
        AzureRuntimeCompositionStepKind kind,
        string purpose,
        params string[] dependsOn) => new()
        {
            Name = name,
            Kind = kind,
            Purpose = purpose,
            DependsOn = dependsOn ?? Array.Empty<string>()
        };

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }
}
