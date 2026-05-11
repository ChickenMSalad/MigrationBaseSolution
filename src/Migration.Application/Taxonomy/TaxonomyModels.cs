namespace Migration.Application.Taxonomy;

public sealed class TaxonomyExportRequest
{
    public string TargetType { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public bool IncludeOptions { get; init; } = true;
    public bool IncludeRaw { get; init; } = false;
}

public sealed class TaxonomyExportResult
{
    public string TargetType { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public int FieldCount { get; init; }
    public int OptionCount { get; init; }
}

public sealed class TaxonomyWorkbook
{
    public string TargetType { get; init; } = string.Empty;
    public List<TaxonomyField> Fields { get; init; } = new();
    public List<TaxonomyOption> Options { get; init; } = new();
    public string? RawJson { get; init; }
}

public sealed class TaxonomyField
{
    public string TargetType { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool Required { get; init; }
    public bool Searchable { get; init; }
    public bool MultiValue { get; init; }
    public string? GroupName { get; init; }
    public string? Status { get; init; }
    public int? SortOrder { get; init; }
}

public sealed class TaxonomyOption
{
    public string TargetType { get; init; } = string.Empty;
    public string FieldId { get; init; } = string.Empty;
    public string FieldName { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool Selectable { get; init; } = true;
    public int? SortOrder { get; init; }
    public string? ParentOptionId { get; init; }
    public string? LinkedOptionIds { get; init; }
}
