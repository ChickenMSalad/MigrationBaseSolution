namespace Migration.Domain.Models;

public sealed class RenditionRecord
{
    public required string Name { get; init; }
    public string? ContentType { get; init; }
    public string? Uri { get; init; }
}
