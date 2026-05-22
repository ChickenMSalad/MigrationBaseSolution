using System;
using System.Collections.Generic;
using System.Reflection;

namespace Migration.Shared.Workflows.AemToAprimo.Models
{
    public static class AssetMetadataFactory
    {
        public static AssetMetadata FromExcelRow(Dictionary<string, string> rowData)
        {
            if (rowData == null)
                throw new ArgumentNullException(nameof(rowData));

            var metadata = new AssetMetadata();
            var type = typeof(AssetMetadata);

            foreach (var kvp in rowData)
            {
                // Excel header must match property name
                var prop = type.GetProperty(
                    kvp.Key,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop == null)
                    continue; // Column not part of AssetMetadata

                if (!prop.CanWrite)
                    continue;

                var value = kvp.Value;

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                // All your properties are string?, so this is safe
                prop.SetValue(metadata, value.Trim());
            }

            return metadata;
        }
    }
}
