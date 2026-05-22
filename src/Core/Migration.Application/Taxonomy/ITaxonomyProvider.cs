namespace Migration.Application.Taxonomy;

public interface ITaxonomyProvider
{
    string TargetType { get; }
    Task<TaxonomyWorkbook> GetTaxonomyAsync(TaxonomyExportRequest request, CancellationToken cancellationToken);
}
