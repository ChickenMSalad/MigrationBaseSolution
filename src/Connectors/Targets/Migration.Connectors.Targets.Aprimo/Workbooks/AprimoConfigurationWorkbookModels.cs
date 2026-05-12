using System.Collections.ObjectModel;

namespace Migration.Connectors.Targets.Aprimo.Workbooks;

public sealed record AprimoConfigurationWorkbookCredentials(
    string SubDomain,
    string ClientId,
    string ClientSecret);

public sealed record AprimoConfigurationWorkbookRequest(
    AprimoConfigurationWorkbookCredentials Credentials,
    AprimoConfigurationWorkbookExportOptions? ExportOptions = null,
    string? ExistingWorkbookWithNotesPath = null);

public sealed record AprimoConfigurationWorkbookResult(
    string FileName,
    string ContentType,
    int WorksheetCount,
    IReadOnlyDictionary<string, int> RowCounts);

public sealed class AprimoConfigurationWorkbookExportOptions
{
    public bool UserGroups { get; init; } = true;
    public bool ClassificationPermissions { get; init; } = true;
    public bool FunctionalPermissions { get; init; } = true;
    public bool FieldGroups { get; init; } = true;
    public bool FieldDefinitions { get; init; } = true;
    public bool Classifications { get; init; } = true;
    public bool ContentTypes { get; init; } = true;
    public bool Settings { get; init; } = true;
    public bool Rules { get; init; } = true;
    public bool Translations { get; init; } = true;
    public bool Watermarks { get; init; } = true;
    public bool Languages { get; init; } = false;

    public static AprimoConfigurationWorkbookExportOptions Defaults { get; } = new();

    public IReadOnlyDictionary<string, bool> ToLegacySelectionMap() =>
        new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["userGroups"] = UserGroups,
            ["classificationPermissions"] = ClassificationPermissions,
            ["functionalPermissions"] = FunctionalPermissions,
            ["fieldGroups"] = FieldGroups,
            ["fieldDefinitions"] = FieldDefinitions,
            ["classifications"] = Classifications,
            ["contentTypes"] = ContentTypes,
            ["settings"] = Settings,
            ["rules"] = Rules,
            ["translations"] = Translations,
            ["watermarks"] = Watermarks,
            ["languages"] = Languages
        });
}

public interface IAprimoConfigurationWorkbookService
{
    Task<AprimoConfigurationWorkbookResult> GenerateAsync(
        AprimoConfigurationWorkbookRequest request,
        Stream outputStream,
        CancellationToken cancellationToken = default);
}
