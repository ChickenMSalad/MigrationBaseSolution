namespace MigrationBase.Core.Cloud.Azure.Deployment.Pipeline;

public sealed class AzureDeploymentPipelineProfileRegistry : IAzureDeploymentPipelineProfileRegistry
{
    private readonly IReadOnlyList<AzureDeploymentPipelineProfile> _profiles;

    public AzureDeploymentPipelineProfileRegistry(IEnumerable<AzureDeploymentPipelineProfile> profiles)
    {
        _profiles = profiles?.ToArray() ?? Array.Empty<AzureDeploymentPipelineProfile>();
    }

    public IReadOnlyList<AzureDeploymentPipelineProfile> GetProfiles() => _profiles;

    public AzureDeploymentPipelineProfile? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _profiles.FirstOrDefault(profile =>
            string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        foreach (var profile in _profiles)
        {
            if (profile is null)
            {
                errors.Add("Pipeline profile cannot be null.");
                continue;
            }

            errors.AddRange(profile.Validate());
        }

        var duplicateNames = _profiles
            .Where(profile => profile is not null && !string.IsNullOrWhiteSpace(profile.Name))
            .GroupBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicateName in duplicateNames)
        {
            errors.Add($"Duplicate pipeline profile name: {duplicateName}");
        }

        return errors;
    }
}
