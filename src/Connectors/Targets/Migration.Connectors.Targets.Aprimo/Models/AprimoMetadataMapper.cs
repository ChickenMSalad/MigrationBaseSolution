using Migration.Shared.Workflows.AemToAprimo.Models;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Migration.Connectors.Targets.Aprimo.Models
{
    public static class AprimoMetadataMapper
    {
        public static (List<(string Field, string Value, string Type)> FromExcel,
                       List<(string Field, string Type, PropertyInfo Prop)> FromJsonSidecar)
            GetStampPlan(AssetMetadata metadata)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            var fromExcel = new List<(string, string, string)>();
            var fromSidecar = new List<(string, string, PropertyInfo)>();

            foreach (var prop in typeof(AssetMetadata).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = prop.GetCustomAttribute<AprimoFieldAttribute>(inherit: true);
                if (attr == null) continue;

                if (attr.Source == MetadataValueSource.JsonSidecar)
                {
                    // Value will be loaded later from metadata.json
                    fromSidecar.Add((attr.FieldName, attr.DataType, prop));
                    continue;
                }

                // Excel (default)
                var raw = prop.GetValue(metadata);
                var value = raw?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                    fromExcel.Add((attr.FieldName, value, attr.DataType));
            }

            return (fromExcel, fromSidecar);
        }
    }
}
