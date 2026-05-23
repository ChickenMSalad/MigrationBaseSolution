namespace MigrationBase.Core.Cloud.Azure.Deployment.Pipeline;

public sealed record AzureDeploymentPipelineProfile(
    string Name,
    string WorkloadName,
    IReadOnlyList<AzureDeploymentPipelineStage> Stages)
{
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Pipeline profile name is required.");
        }

        if (string.IsNullOrWhiteSpace(WorkloadName))
        {
            errors.Add("Workload name is required.");
        }

        if (Stages is null || Stages.Count == 0)
        {
            errors.Add("At least one pipeline stage is required.");
            return errors;
        }

        foreach (var stage in Stages)
        {
            if (stage is null || !stage.IsValid)
            {
                errors.Add("All pipeline stages must define name, environment, and deployment ring.");
            }
        }

        return errors;
    }
}
