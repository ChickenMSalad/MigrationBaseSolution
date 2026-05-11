using System.Collections.Generic;

namespace Migration.Shared.Configuration.Infrastructure;

public class ExportOptions
{
    public string Folder { get; set; } = "";
    public List<string> Folders { get; set; } = new();
    public bool Recursive { get; set; } = true;
}
