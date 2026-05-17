namespace Migration.ControlPlane.Auth;

public sealed class EndpointPolicyInventoryService : IEndpointPolicyInventoryService
{
    public EndpointPolicyInventorySnapshot GetSnapshot()
    {
        var items = BuildItems();

        var warnings = new List<string>
        {
            "This inventory is advisory only; endpoint enforcement is not enabled by this set.",
            "Route groups should be reviewed before policy enforcement is applied globally."
        };

        return new EndpointPolicyInventorySnapshot(
            GeneratedUtc: DateTimeOffset.UtcNow,
            Items: items,
            ReadOnlyCount: items.Count(x => !x.MutatesState),
            MutatingCount: items.Count(x => x.MutatesState),
            CredentialSensitiveCount: items.Count(x => x.ExposesSecretsOrCredentials),
            OperationallySensitiveCount: items.Count(x => x.OperationallySensitive),
            Warnings: warnings);
    }

    private static IReadOnlyList<EndpointPolicyInventoryItem> BuildItems() =>
    [
        new(
            Area: "System",
            RoutePrefix: "/system",
            RecommendedPolicy: "AdminApi.Read",
            RequiredScope: "admin.read",
            MutatesState: false,
            ExposesSecretsOrCredentials: false,
            OperationallySensitive: false,
            Notes: "Basic system metadata and health-style endpoints."),
        new(
            Area: "Projects",
            RoutePrefix: "/api/projects",
            RecommendedPolicy: "AdminApi.Write",
            RequiredScope: "admin.write",
            MutatesState: true,
            ExposesSecretsOrCredentials: false,
            OperationallySensitive: false,
            Notes: "Project creation and update paths are mutating."),
        new(
            Area: "Runs",
            RoutePrefix: "/api/runs",
            RecommendedPolicy: "AdminApi.Write",
            RequiredScope: "admin.write",
            MutatesState: true,
            ExposesSecretsOrCredentials: false,
            OperationallySensitive: true,
            Notes: "Run creation/execution paths are operationally sensitive."),
        new(
            Area: "Credentials",
            RoutePrefix: "/api/credentials",
            RecommendedPolicy: "AdminApi.Credentials",
            RequiredScope: "admin.credentials",
            MutatesState: true,
            ExposesSecretsOrCredentials: true,
            OperationallySensitive: true,
            Notes: "Credential metadata, binding, diagnostics, and value probes require dedicated credential policy."),
        new(
            Area: "Artifacts",
            RoutePrefix: "/api/artifacts",
            RecommendedPolicy: "AdminApi.Write",
            RequiredScope: "admin.write",
            MutatesState: true,
            ExposesSecretsOrCredentials: false,
            OperationallySensitive: true,
            Notes: "Artifact write/delete operations mutate control-plane state."),
        new(
            Area: "Queue",
            RoutePrefix: "/api/cloud/queue",
            RecommendedPolicy: "AdminApi.Operations",
            RequiredScope: "admin.operations",
            MutatesState: true,
            ExposesSecretsOrCredentials: false,
            OperationallySensitive: true,
            Notes: "Queue probes can dispatch/receive/poll messages depending on configuration."),
        new(
            Area: "Audit",
            RoutePrefix: "/api/cloud/audit",
            RecommendedPolicy: "AdminApi.Operations",
            RequiredScope: "admin.operations",
            MutatesState: true,
            ExposesSecretsOrCredentials: false,
            OperationallySensitive: true,
            Notes: "Audit probes write diagnostic records."),
        new(
            Area: "Telemetry",
            RoutePrefix: "/api/cloud/telemetry",
            RecommendedPolicy: "AdminApi.Operations",
            RequiredScope: "admin.operations",
            MutatesState: true,
            ExposesSecretsOrCredentials: false,
            OperationallySensitive: true,
            Notes: "Telemetry probes write diagnostic events."),
        new(
            Area: "Readiness",
            RoutePrefix: "/api/cloud/operations",
            RecommendedPolicy: "AdminApi.Operations",
            RequiredScope: "admin.operations",
            MutatesState: false,
            ExposesSecretsOrCredentials: false,
            OperationallySensitive: true,
            Notes: "Operational readiness exposes deployment posture."),
        new(
            Area: "Cloud diagnostics",
            RoutePrefix: "/api/cloud",
            RecommendedPolicy: "AdminApi.Operations",
            RequiredScope: "admin.operations",
            MutatesState: false,
            ExposesSecretsOrCredentials: false,
            OperationallySensitive: true,
            Notes: "General cloud diagnostics and planning endpoints.")
    ];
}
