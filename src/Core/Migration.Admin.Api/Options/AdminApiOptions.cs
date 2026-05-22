namespace Migration.Admin.Api.Options;

public sealed class AdminApiOptions
{
    public const string SectionName = "AdminApi";
    public string StorageRoot { get; set; } = "Runtime/admin-api";
    public bool AllowInProcessExecution { get; set; }
}
