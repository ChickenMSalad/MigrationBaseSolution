using Migration.Connectors.Targets.Aprimo.Models.Aprimo;
using System;
using System.Linq;

namespace Migration.Connectors.Targets.Aprimo.Extensions
{
    public static class AprimoRecordExtensions
    {
        public static string? GetSingleValue(this AprimoRecord record, string fieldName)
        {
            var field = record?.Embedded?.Fields?.Items?
                .FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));

            return field?.LocalizedValues?.FirstOrDefault()?.Value;
        }

        public static string[] GetMultiValues(this AprimoRecord record, string fieldName)
        {
            var field = record?.Embedded?.Fields?.Items?
                .FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));

            return field?.LocalizedValues?.FirstOrDefault()?.Values?.ToArray() ?? Array.Empty<string>();
        }
    }
}
