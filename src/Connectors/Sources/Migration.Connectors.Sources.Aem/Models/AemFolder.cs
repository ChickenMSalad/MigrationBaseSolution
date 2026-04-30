
using System.Collections.Generic;

namespace Migration.Connectors.Sources.Aem.Models;

public sealed record AemFolder(
    string Path,
    IReadOnlyList<AemAsset> ChildAssets,
    IReadOnlyList<string> ChildFolderPaths,
    string Error
);
