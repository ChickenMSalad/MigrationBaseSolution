namespace MigrationBase.Core.Cloud.Azure.EndToEndValidation;

public interface IAzureEndToEndEvidenceReportBuilder
{
    AzureEndToEndEvidenceReport Build(AzureEndToEndEvidenceReportRequest request);
}
