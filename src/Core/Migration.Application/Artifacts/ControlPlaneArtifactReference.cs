namespace Migration.Application.Artifacts;

public static class ControlPlaneArtifactReference
{
    public const string Scheme = "artifact://";

    public static string Create(string artifactId)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            throw new ArgumentException("Artifact id is required.", nameof(artifactId));
        }

        return Scheme + artifactId.Trim();
    }

    public static bool TryParse(string? value, out string artifactId)
    {
        artifactId = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        artifactId = trimmed[Scheme.Length..].Trim();
        return !string.IsNullOrWhiteSpace(artifactId);
    }
}
