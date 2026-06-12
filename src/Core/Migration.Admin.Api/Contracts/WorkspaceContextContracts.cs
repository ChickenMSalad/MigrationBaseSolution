namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Cloud-facing workspace/tenant context. This is intentionally a lightweight
/// contract for P1 so the API and frontend can begin carrying workspace shape
/// before storage partitioning and auth enforcement are introduced.
/// </summary>
public sealed record WorkspaceContextDescriptor(
    string WorkspaceId,
    string DisplayName,
    string TenantMode,
    bool IsDefaultWorkspace,
    bool IsTenantEnforced,
    string? TenantId,
    IReadOnlyList<string> AllowedConnectorRoles,
    IReadOnlyList<string> Warnings);

public static class WorkspaceTenantModes
{
    public const string SingleTenant = "singleTenant";
    public const string MultiTenant = "multiTenant";
    public const string Development = "development";
}


