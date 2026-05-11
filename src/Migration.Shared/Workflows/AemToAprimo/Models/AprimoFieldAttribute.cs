using System;

namespace Migration.Shared.Workflows.AemToAprimo.Models
{
    public enum MetadataValueSource
    {
        Excel = 0,
        JsonSidecar = 1
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class AprimoFieldAttribute : Attribute
    {
        public string FieldName { get; }
        public string DataType { get; }
        public MetadataValueSource Source { get; }

        public AprimoFieldAttribute(string fieldName, string dataType = "Text", MetadataValueSource source = MetadataValueSource.Excel)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Aprimo field name is required.", nameof(fieldName));

            FieldName = fieldName;
            DataType = dataType ?? "Text";
            Source = source;
        }
    }
}
