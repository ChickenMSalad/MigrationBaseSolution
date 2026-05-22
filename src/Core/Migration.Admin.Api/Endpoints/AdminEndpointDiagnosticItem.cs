namespace Migration.Admin.Api.Endpoints;

public sealed class AdminEndpointDiagnosticItem
{
    public string RoutePattern { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public IReadOnlyCollection<string> Methods { get; init; } =
        Array.Empty<string>();
}
