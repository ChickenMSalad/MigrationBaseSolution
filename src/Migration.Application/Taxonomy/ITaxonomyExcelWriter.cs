namespace Migration.Application.Taxonomy;

public interface ITaxonomyExcelWriter
{
    Task WriteAsync(TaxonomyWorkbook workbook, string outputPath, CancellationToken cancellationToken);
}
