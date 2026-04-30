using System;
using System.Collections.Generic;
using System.Reflection;

namespace Migration.Shared.Workflows.AemToAprimo.Models
{
    public static class RowDataMetadataPopulator
    {
        /// <summary>
        /// Populates existing rowData columns (keys) with values from AssetMetadata.
        /// Assumes the columns already exist in rowData. Does not add new keys.
        /// </summary>
        public static void PopulateExistingColumnsFromMetadata(
            Dictionary<string, string> rowData,
            AssetMetadata metadata,
            bool overwriteExisting = true,
            bool writeEmptyValues = false)
        {
            if (rowData == null) throw new ArgumentNullException(nameof(rowData));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            foreach (var prop in typeof(AssetMetadata).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;
                if (prop.PropertyType != typeof(string)) continue;

                var key = prop.Name;

                // Columns already exist: skip if column not present
                if (!rowData.ContainsKey(key))
                    continue;

                if (!overwriteExisting && !string.IsNullOrWhiteSpace(rowData[key]))
                    continue;

                var value = prop.GetValue(metadata) as string;

                if (string.IsNullOrWhiteSpace(value))
                {
                    if (writeEmptyValues)
                        rowData[key] = "";
                    continue;
                }

                rowData[key] = value.Trim();
            }
        }
    }
}
