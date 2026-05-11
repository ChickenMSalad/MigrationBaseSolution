namespace Migration.Application.Taxonomy;

public sealed class TaxonomyExportService
{
    private readonly IReadOnlyDictionary<string, ITaxonomyProvider> _providers;
    private readonly ITaxonomyExcelWriter _writer;

    public TaxonomyExportService(IEnumerable<ITaxonomyProvider> providers, ITaxonomyExcelWriter writer)
    {
        _providers = providers.ToDictionary(x => x.TargetType, StringComparer.OrdinalIgnoreCase);
        _writer = writer;
    }

    public async Task<TaxonomyExportResult> ExportToExcelAsync(TaxonomyExportRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TargetType))
        {
            throw new ArgumentException("TargetType is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            throw new ArgumentException("OutputPath is required.", nameof(request));
        }

        if (!_providers.TryGetValue(request.TargetType, out var provider))
        {
            throw new InvalidOperationException($"No taxonomy provider is registered for target '{request.TargetType}'.");
        }

        var workbook = await provider.GetTaxonomyAsync(request, cancellationToken).ConfigureAwait(false);
        await _writer.WriteAsync(workbook, request.OutputPath, cancellationToken).ConfigureAwait(false);

        return new TaxonomyExportResult
        {
            TargetType = workbook.TargetType,
            OutputPath = request.OutputPath,
            FieldCount = workbook.Fields.Count,
            OptionCount = workbook.Options.Count
        };
    }
}
