using Migration.Connectors.Sources.Aem.Rules;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Migration.Connectors.Sources.Aem.Configuration
{
    public static class FolderClassificationRuleLoader
    {
        // rules for mapping
        public static IReadOnlyList<FolderClassificationRule> LoadFolderMapping(
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("folderMapping.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource 'folderMapping.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<FolderClassificationRuleSet>(json);
            if (ruleSet?.Rules == null || ruleSet.Rules.Count == 0)
                throw new InvalidOperationException("No folder classification rules were loaded.");

            return ruleSet.Rules;
        }


        public static VendorSubtypeRuleSet Load3PVendorAndSubtypeMapping(
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("3PVendorAndSubtypeMapping.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource '3PVendorAndSubtypeMapping.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<VendorSubtypeRuleSet>(json);
            if (ruleSet == null || ruleSet.Rules.Count() == 0)
                throw new InvalidOperationException("No 3PVendorAndSubtypeMapping rules were loaded.");

            return ruleSet;
        }
        public static IReadOnlyList<SubtypeRule> LoadP1Subtype(
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("1PSubtypeFromFileName.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource '1PSubtypeFromFileName.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<List<SubtypeRule>>(json);
            if (ruleSet == null || ruleSet.Count == 0)
                throw new InvalidOperationException("No sub folder classification rules were loaded.");

            return ruleSet;
        }

        public static List<FilenameCategoryRule> LoadGLBProductCatRules(
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("GLB_ProdCatRules.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource 'GLB_ProdCatRules.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<FilenameCategoryRules>(json);
            if (ruleSet == null || ruleSet.Rules.Count() == 0)
                throw new InvalidOperationException("No GLB_ProdCatRules rules were loaded.");

            return ruleSet.Rules;
        }

        public static List<ProductCategoryStudioPhotographyRule> LoadStudioPhotographyProductCatRules(
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("productCatStudioPhotography.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource 'productCatStudioPhotography.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<ProductCategoryStudioPhotographyRuleSet>(json);
            if (ruleSet == null || ruleSet.Rules.Count() == 0)
                throw new InvalidOperationException("No productCatStudioPhotography rules were loaded.");

            return ruleSet.Rules;
        }

        public static List<ImagePlaceholderProductCategoryRule> LoadImagePlaceholderProductCatRules(
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("productCatImagePlaceholder.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource 'productCatImagePlaceholder.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<ImagePlaceholderProductCategoryRuleSet>(json);
            if (ruleSet == null || ruleSet.Rules.Count() == 0)
                throw new InvalidOperationException("No productCatImagePlaceholder rules were loaded.");

            return ruleSet.Rules;
        }


        public static List<SeriesFPOProductCategoryRule> LoadSeriesFPOProductCatRules(
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("productCatSeriesFPO.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource 'productCatSeriesFPO.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<SeriesFPOProductCategoryRuleSet>(json);
            if (ruleSet == null || ruleSet.Rules.Count() == 0)
                throw new InvalidOperationException("No productCatSeriesFPO rules were loaded.");

            return ruleSet.Rules;
        }

        public static List<ProductCategoryAFIVideoRule> LoadAFIVideoProductCatRules(
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("afiProductVideos.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource 'afiProductVideos.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<ProductCategoryAFIVideoRuleSet>(json);
            if (ruleSet == null || ruleSet.Rules.Count() == 0)
                throw new InvalidOperationException("No afiProductVideos rules were loaded.");

            return ruleSet.Rules;
        }

        public static List<ProductCategoryAHSVideoRule> LoadAHSVideoProductCatRules(
            Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("ahsProductVideos.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource 'ahsProductVideos.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<ProductCategoryAHSVideoRuleSet>(json);
            if (ruleSet == null || ruleSet.Rules.Count() == 0)
                throw new InvalidOperationException("No ahsProductVideos rules were loaded.");

            return ruleSet.Rules;
        }

        public static List<ItemStatusRule> LoadItemStatusRules(
    Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("itemStatusRules.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource 'itemStatusRules.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<ItemStatusRuleSet>(json);
            if (ruleSet == null || ruleSet.Rules.Count() == 0)
                throw new InvalidOperationException("No itemStatus rules were loaded.");

            return ruleSet.Rules;
        }

        public static List<InlineFinishCGIRule> LoadInlineFinishCGIRules(
    Assembly? assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("cgiProductFinish.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    "Embedded resource 'cgiProductFinish.json' was not found. " +
                    "Ensure the file exists and its Build Action is set to Embedded Resource.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Unable to open resource stream: {resourceName}");

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var ruleSet = JsonConvert.DeserializeObject<InlineFinishCGIRuleSet>(json);
            if (ruleSet == null || ruleSet.Rules.Count() == 0)
                throw new InvalidOperationException("No cgiProductFinish rules were loaded.");

            return ruleSet.Rules;
        }

        // rule finders
        public static FolderClassificationRule? GetRuleForPath(
            IEnumerable<FolderClassificationRule> rules,
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            return rules
                .Where(r => !string.IsNullOrWhiteSpace(r.AemFolder))
                .Select(r => new
                {
                    Rule = r,
                    Folder = Normalize(r.AemFolder)
                })
                .Where(x => path.IndexOf(x.Folder, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(x => x.Folder.Length) // longest match wins
                .Select(x => x.Rule)
                .FirstOrDefault();
        }

        private static string Normalize(string value)
        {
            value = value.Trim();

            value = value.Replace("\\", "/");

            if (!value.StartsWith("/", StringComparison.Ordinal))
                value = "/" + value;

            if (value.Length > 1 && value.EndsWith("/", StringComparison.Ordinal))
                value = value.TrimEnd('/');

            return value;
        }

        public static SubtypeRule? ResolveFromFileName(IEnumerable<SubtypeRule> rules, string fileName)
        {
            if (rules == null) return null;
            var ruleList = rules.Where(r => r != null).ToList();
            if (ruleList.Count == 0) return null;

            if (string.IsNullOrWhiteSpace(fileName)) return null;

            // normalize: use the base file name without extension
            var baseName = Path.GetFileNameWithoutExtension(fileName) ?? "";
            if (string.IsNullOrWhiteSpace(baseName)) return null;

            static bool ContainsCI(string haystack, string needle) =>
                haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

            static bool EndsWithCI(string haystack, string suffix) =>
                haystack.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

            static IEnumerable<string> Clean(IEnumerable<string>? values) =>
                (values ?? Enumerable.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim());

            bool Matches(SubtypeRule r)
            {
                // NOTE: Filename-only resolver does NOT return AI rules
                if (r.RequiresAprimoAI) return false;

                var endsWithAny = Clean(r.EndsWithAny).ToList();
                var notEndsWithAny = Clean(r.NotEndsWithAny).ToList();
                var containsAny = Clean(r.ContainsAny).ToList();
                var notContainsAny = Clean(r.NotContainsAny).ToList();

                // EndsWithAny: if provided, must match at least one
                if (endsWithAny.Count > 0 && !endsWithAny.Any(s => EndsWithCI(baseName, s)))
                    return false;

                // NotEndsWithAny: if provided, must match none
                if (notEndsWithAny.Count > 0 && notEndsWithAny.Any(s => EndsWithCI(baseName, s)))
                    return false;

                // ContainsAny: if provided, must contain at least one
                if (containsAny.Count > 0 && !containsAny.Any(s => ContainsCI(baseName, s)))
                    return false;

                // NotContainsAny: if provided, must contain none
                if (notContainsAny.Count > 0 && notContainsAny.Any(s => ContainsCI(baseName, s)))
                    return false;

                return true;
            }

            // 1) explicit rules first (non-default), ordered by priority
            var explicitMatch = ruleList
                .Where(r => !r.IsDefault)
                .OrderByDescending(r => r.Priority)
                .FirstOrDefault(Matches);

            if (explicitMatch != null)
                return explicitMatch;

            // 2) default rule (no explicit suffix) => Lifestyle (per your requirement)
            var defaultRule = ruleList
                .Where(r => r.IsDefault)
                .OrderByDescending(r => r.Priority)
                .FirstOrDefault();

            return defaultRule;
        }


        public static VendorSubtypeRule? Resolve(VendorSubtypeRuleSet ruleSet, string path)
        {
            if (ruleSet?.Rules == null || ruleSet.Rules.Count == 0) return null;
            if (string.IsNullOrWhiteSpace(path)) return null;

            // Case-insensitive contains
            bool ContainsCI(string haystack, string needle) =>
                haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

            var matches = ruleSet.Rules
                .Where(r => !string.IsNullOrWhiteSpace(r.AemFolderToken))
                .Where(r => ContainsCI(path, r.AemFolderToken.Trim()));

            return ruleSet.PreferMostSpecificMatch
                ? matches.OrderByDescending(r => r.AemFolderToken.Trim().Length).FirstOrDefault()
                : matches.FirstOrDefault();
        }

        public static string? GetCategoryFromFilename(IEnumerable<FilenameCategoryRule> rules, string filename)
        {
            foreach (var rule in rules)
            {
                if (Regex.IsMatch(filename, rule.Pattern, RegexOptions.IgnoreCase))
                    return rule.Category;
            }

            return null;
        }

        public static ProductCategoryStudioPhotographyRule?
            GetStudioPhotographyProductCategoryRule(
                IEnumerable<ProductCategoryStudioPhotographyRule> rules,
                string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            return rules
                .Where(r => !string.IsNullOrWhiteSpace(r.AemFolderName))
                .Select(r => new
                {
                    Rule = r,
                    Folder = r.AemFolderName.Trim()
                })
                .Where(x =>
                    assetPath.IndexOf(
                        x.Folder,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(x => x.Folder.Length) // longest match wins
                .Select(x => x.Rule)
                .FirstOrDefault();
        }

        public static ImagePlaceholderProductCategoryRule? GetImagePlaceholderRule(
            IEnumerable<ImagePlaceholderProductCategoryRule> rules,
            string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            return rules
                .Where(r => !string.IsNullOrWhiteSpace(r.AemFolderName))
                .Select(r => new { Rule = r, Folder = r.AemFolderName.Trim() })
                .Where(x => assetPath.IndexOf(x.Folder, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(x => x.Folder.Length) // longest match wins
                .Select(x => x.Rule)
                .FirstOrDefault();
        }

        public static SeriesFPOProductCategoryRule? GetSeriesFPORule(
            IEnumerable<SeriesFPOProductCategoryRule> rules,
            string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            return rules
                .Where(r => !string.IsNullOrWhiteSpace(r.AemFolderName))
                .Select(r => new { Rule = r, Folder = r.AemFolderName.Trim() })
                .Where(x => assetPath.IndexOf(x.Folder, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(x => x.Folder.Length) // longest match wins
                .Select(x => x.Rule)
                .FirstOrDefault();
        }

        public static ProductCategoryAFIVideoRule?
            GetAFIVideoProductCategoryRule(
                IEnumerable<ProductCategoryAFIVideoRule> rules,
                string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            return rules
                .Where(r => !string.IsNullOrWhiteSpace(r.AemFolderName))
                .Select(r => new
                {
                    Rule = r,
                    Folder = r.AemFolderName.Trim()
                })
                .Where(x =>
                    assetPath.IndexOf(
                        x.Folder,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(x => x.Folder.Length) // longest match wins
                .Select(x => x.Rule)
                .FirstOrDefault();
        }

        public static ProductCategoryAHSVideoRule?
            GetAHSVideoProductCategoryRule(
                IEnumerable<ProductCategoryAHSVideoRule> rules,
                string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            return rules
                .Where(r => !string.IsNullOrWhiteSpace(r.AemFolderName))
                .Select(r => new
                {
                    Rule = r,
                    Folder = r.AemFolderName.Trim()
                })
                .Where(x =>
                    assetPath.IndexOf(
                        x.Folder,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(x => x.Folder.Length) // longest match wins
                .Select(x => x.Rule)
                .FirstOrDefault();
        }


        public static InlineFinishCGIRule?
            GetInlineFinishCGIRule(
                IEnumerable<InlineFinishCGIRule> rules,
                string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            return rules
                .Where(r => !string.IsNullOrWhiteSpace(r.AemFolderName))
                .Select(r => new
                {
                    Rule = r,
                    Folder = r.AemFolderName.Trim()
                })
                .Where(x =>
                    assetPath.IndexOf(
                        x.Folder,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(x => x.Folder.Length) // longest match wins
                .Select(x => x.Rule)
                .FirstOrDefault();
        }

        public static ItemStatusRule?
            GetItemStatusRule(
                IEnumerable<ItemStatusRule> rules,
                string? itemStatus)
        {
            if (rules == null)
                return null;

            return rules.FirstOrDefault(r =>
                itemStatus == null
                    ? r.CurrentValue == null
                    : string.Equals(r.CurrentValue, itemStatus, StringComparison.Ordinal)
            );
        }
    }
}
