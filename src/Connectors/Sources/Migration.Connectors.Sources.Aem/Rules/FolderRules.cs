using System.Collections.Generic;

namespace Migration.Connectors.Sources.Aem.Rules
{
    public sealed class FolderClassificationRuleSet
    {
        public string MatchMode { get; set; } = "PathContainsFolder";
        public string MatchStrategy { get; set; } = "LongestFolderFirst";
        public List<FolderClassificationRule> Rules { get; set; } = new();
    }

    public sealed class FolderClassificationRule
    {
        public string AemFolder { get; set; } = default!;
        public string AssetType { get; set; } = default!;
        public string AssetSubtype { get; set; } = default!;
        public bool RequiresAdditionalLogic { get; set; }
        public string? Notes { get; set; }
    }
}
