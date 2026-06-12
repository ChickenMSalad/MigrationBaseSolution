namespace Migration.Admin.Api.Contracts;

/// <summary>
/// Safe authorization policy plan for future cloud auth enforcement.
/// This is planning/diagnostic only and does not enforce authorization yet.
/// </summary>
public sealed record AuthorizationPolicyPlanDescriptor(
    string EnvironmentName,
    string AuthMode,
    bool AuthRequired,
    bool TenantEnforced,
    string? Authority,
    string? Audience,
    IReadOnlyList<AuthorizationRoleDescriptor> Roles,
    IReadOnlyList<AuthorizationScopeDescriptor> Scopes,
    IReadOnlyList<AuthorizationRoutePolicyDescriptor> RoutePolicies,
    IReadOnlyList<string> Warnings);

public sealed record AuthorizationRoleDescriptor(
    string Role,
    string DisplayName,
    string Description);

public sealed record AuthorizationScopeDescriptor(
    string Scope,
    string DisplayName,
    string Description);

public sealed record AuthorizationRoutePolicyDescriptor(
    string RoutePattern,
    string Policy,
    IReadOnlyList<string> RequiredRoles,
    IReadOnlyList<string> RequiredScopes);

public static class AuthorizationRoleNames
{
    public const string Reader = "migration.reader";
    public const string Operator = "migration.operator";
    public const string Admin = "migration.admin";
    public const string Auditor = "migration.auditor";
}

public static class AuthorizationScopeNames
{
    public const string Read = "migration.read";
    public const string Write = "migration.write";
    public const string Execute = "migration.execute";
    public const string Admin = "migration.admin";
    public const string Audit = "migration.audit";
}


