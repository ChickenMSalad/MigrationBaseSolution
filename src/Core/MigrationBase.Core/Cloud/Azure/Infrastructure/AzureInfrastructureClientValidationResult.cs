namespace MigrationBase.Core.Cloud.Azure.Infrastructure;

public sealed class AzureInfrastructureClientValidationResult
{
    public IList<AzureInfrastructureClientValidationIssue> Issues { get; } = new List<AzureInfrastructureClientValidationIssue>();

    public bool IsValid => Issues.Count == 0;

    public void AddIssue(string code, string message, string? clientName = null, string? settingName = null)
    {
        Issues.Add(new AzureInfrastructureClientValidationIssue
        {
            Code = code,
            Message = message,
            ClientName = clientName,
            SettingName = settingName
        });
    }
}
