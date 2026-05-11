using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Migration.Connectors.Targets.Aprimo.Files
{
    public static class DataTableConverter
    {
        public static DataTable ToDataTable<T>(IEnumerable<T> items)
        {
            var table = new DataTable(typeof(T).Name);
            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Create columns
            foreach (var prop in props)
            {
                Type colType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                table.Columns.Add(prop.Name, colType);
            }

            // Fill rows
            foreach (var item in items)
            {
                var row = table.NewRow();

                foreach (var prop in props)
                {
                    object? value = prop.GetValue(item, null);
                    row[prop.Name] = value ?? DBNull.Value;
                }

                table.Rows.Add(row);
            }

            return table;
        }
    }

}
