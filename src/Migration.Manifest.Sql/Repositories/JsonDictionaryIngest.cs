using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Migration.Manifest.Sql.Repositories
{

    public static class JsonDictionaryIngest
    {
        public static void IngestFile(
            string filePath,
            string connectionString,
            string stagingTable = "dbo.MappingHelperObjects_Staging",
            int batchSize = 5000)
        {
            var table = CreateStagingDataTable();

            using (var stream = File.OpenRead(filePath))
            using (var sr = new StreamReader(stream))
            using (var reader = new JsonTextReader(sr))
            {
                reader.SupportMultipleContent = false;

                if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
                    throw new Exception("JSON must be an object at root.");

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        string dictKey = reader.Value.ToString();

                        reader.Read(); // Move to value

                        var obj = Newtonsoft.Json.Linq.JToken.ReadFrom(reader);
                        string jsonBody = obj.ToString(Formatting.None);

                        byte[] hash;
                        using (var sha = SHA256.Create())
                        {
                            hash = sha.ComputeHash(Encoding.UTF8.GetBytes(jsonBody));
                        }

                        var row = table.NewRow();
                        row["DictKey"] = dictKey;
                        row["JsonBody"] = jsonBody;
                        row["SourceFile"] = Path.GetFileName(filePath);
                        row["JsonHash"] = hash;
                        table.Rows.Add(row);

                        if (table.Rows.Count >= batchSize)
                        {
                            BulkCopy(connectionString, stagingTable, table);
                            table.Clear();
                        }
                    }
                }
            }

            if (table.Rows.Count > 0)
            {
                BulkCopy(connectionString, stagingTable, table);
                table.Clear();
            }
        }

        private static DataTable CreateStagingDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("DictKey", typeof(string));
            dt.Columns.Add("JsonBody", typeof(string));
            dt.Columns.Add("SourceFile", typeof(string));
            dt.Columns.Add("JsonHash", typeof(byte[]));
            return dt;
        }

        private static void BulkCopy(string connectionString, string stagingTable, DataTable dt)
        {
            using (var con = new SqlConnection(connectionString))
            {
                con.Open();

                using (var bulk = new SqlBulkCopy(con, SqlBulkCopyOptions.TableLock, null))
                {
                    bulk.DestinationTableName = stagingTable;
                    bulk.BatchSize = dt.Rows.Count;
                    bulk.BulkCopyTimeout = 0;

                    bulk.ColumnMappings.Add("DictKey", "DictKey");
                    bulk.ColumnMappings.Add("JsonBody", "JsonBody");
                    bulk.ColumnMappings.Add("SourceFile", "SourceFile");
                    bulk.ColumnMappings.Add("JsonHash", "JsonHash");

                    bulk.WriteToServer(dt);
                }
            }
        }
    }


}
