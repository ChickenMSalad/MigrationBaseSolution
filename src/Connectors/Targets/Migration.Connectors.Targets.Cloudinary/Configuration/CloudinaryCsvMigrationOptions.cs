namespace Migration.Connectors.Targets.Cloudinary.Configuration;

public sealed class CloudinaryCsvMigrationOptions
{
    public const string SectionName = "CloudinaryCsvMigration";

    public string? ManifestPath { get; set; }
    public string? MappingPath { get; set; }
    public string? OutputRoot { get; set; }
    public string DefaultRunPrefix { get; set; } = "run";
    public bool ConfirmDeletes { get; set; } = true;
    public bool ConfirmProduction { get; set; } = true;
}
