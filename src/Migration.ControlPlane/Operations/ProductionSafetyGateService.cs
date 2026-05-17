using Migration.ControlPlane.Auth;

namespace Migration.ControlPlane.Operations;

public sealed class ProductionSafetyGateService : IProductionSafetyGateService
{
    private readonly IAuthPolicyReadinessService _authPolicy;
    private readonly ICredentialAccessPolicyReadinessService _credentialAccess;
    private readonly IOperationalReadinessService _operationalReadiness;

    public ProductionSafetyGateService(
        IAuthPolicyReadinessService authPolicy,
        ICredentialAccessPolicyReadinessService credentialAccess,
        IOperationalReadinessService operationalReadiness)
    {
        _authPolicy = authPolicy;
        _credentialAccess = credentialAccess;
        _operationalReadiness = operationalReadiness;
    }

    public ProductionSafetyGateSnapshot GetSnapshot()
    {
        var auth = _authPolicy.GetSnapshot();
        var credentials = _credentialAccess.GetSnapshot();
        var operations = _operationalReadiness.GetSnapshot();

        var gates = new List<ProductionSafetyGate>
        {
            new(
                Name: "auth-policy-readiness",
                Passed: auth.IsReadyForProduction,
                RequiredForProduction: true,
                Description: "Auth policy readiness must pass before production mode.",
                Issues: auth.BlockingIssues),
            new(
                Name: "credential-access-policy-readiness",
                Passed: credentials.IsReadyForProduction,
                RequiredForProduction: true,
                Description: "Credential access policy readiness must pass before production mode.",
                Issues: credentials.BlockingIssues),
            new(
                Name: "operational-readiness",
                Passed: operations.IsOperationallyReady,
                RequiredForProduction: true,
                Description: "Audit, telemetry, and queue operational readiness must pass.",
                Issues: operations.BlockingIssues),
            new(
                Name: "live-queue-execution-readiness",
                Passed: operations.IsReadyForLiveQueueExecution,
                RequiredForProduction: false,
                Description: "Live queue execution requires explicit queue, audit, and telemetry readiness.",
                Issues: operations.QueueExecution.BlockingIssues)
        };

        var blocking = gates
            .Where(x => x.RequiredForProduction && !x.Passed)
            .SelectMany(x => x.Issues.Count > 0 ? x.Issues.Select(issue => $"{x.Name}: {issue}") : [$"{x.Name}: gate failed"])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var warnings = new List<string>();

        foreach (var warning in auth.Warnings)
        {
            AddUnique(warnings, $"auth: {warning}");
        }

        foreach (var warning in credentials.Warnings)
        {
            AddUnique(warnings, $"credentials: {warning}");
        }

        foreach (var warning in operations.Warnings)
        {
            AddUnique(warnings, $"operations: {warning}");
        }

        if (!operations.IsReadyForLiveQueueExecution)
        {
            AddUnique(warnings, "Live queue execution is not allowed by the current readiness posture.");
        }

        return new ProductionSafetyGateSnapshot(
            GeneratedUtc: DateTimeOffset.UtcNow,
            IsProductionReady: blocking.Length == 0,
            IsLiveQueueExecutionAllowed: operations.IsReadyForLiveQueueExecution && blocking.Length == 0,
            Gates: gates,
            BlockingIssues: blocking,
            Warnings: warnings,
            AuthPolicy: auth,
            CredentialAccess: credentials,
            OperationalReadiness: operations);
    }

    private static void AddUnique(List<string> values, string value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            !values.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            values.Add(value);
        }
    }
}
