using System;
using System.Reflection;

namespace Migration.Shared.Workflows.AemToAprimo.Models
{
    public static class AssetMetadataMerge
    {
        /// <summary>
        /// Fills null/empty values on <paramref name="target"/> with values from <paramref name="fallback"/>.
        /// Excel object should be target; sidecar object should be fallback.
        /// </summary>
        public static void MergeMissingValues(AssetMetadata target, AssetMetadata fallback)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (fallback == null) throw new ArgumentNullException(nameof(fallback));

            foreach (var prop in typeof(AssetMetadata).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (prop.PropertyType != typeof(string) && prop.PropertyType != typeof(string)) { /* string? compiles as string */ }

                var current = prop.GetValue(target) as string;
                if (!string.IsNullOrWhiteSpace(current))
                    continue; // Excel already has it

                var fromFallback = prop.GetValue(fallback) as string;
                if (!string.IsNullOrWhiteSpace(fromFallback))
                    prop.SetValue(target, fromFallback);
            }
        }
    }
}
