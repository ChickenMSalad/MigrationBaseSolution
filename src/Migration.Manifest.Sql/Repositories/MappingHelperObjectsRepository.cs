using System;
using Migration.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Manifest.Sql.Repositories
{
    using Microsoft.Data.SqlClient;
    using System;
    using System.Data;

    public sealed class MappingHelperObjectsRepository
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly int _batch;

        public MappingHelperObjectsRepository(string connectionString, string tableName = "ashley.dbo.MappingHelperObjects", int batch = 1)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString is required", nameof(connectionString));

            _connectionString = connectionString;
            _tableName = tableName;
            _batch = batch;
        }

        #region CREATE

        public bool Insert(string dictKey, string jsonBody, string sourceFile = null)
        {
            if (string.IsNullOrWhiteSpace(dictKey))
                throw new ArgumentException("dictKey is required", nameof(dictKey));

            var sql =
                $"INSERT INTO {_tableName} (DictKey, JsonBody, SourceFile, IngestedAt) " +
                $"VALUES (@DictKey, @JsonBody, @SourceFile, SYSDATETIMEOFFSET());";

            try
            {
                using (var con = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@DictKey", SqlDbType.NVarChar, 450).Value = dictKey;
                    cmd.Parameters.Add("@JsonBody", SqlDbType.NVarChar).Value = jsonBody ?? string.Empty;
                    cmd.Parameters.Add("@SourceFile", SqlDbType.NVarChar, 260).Value =
                        (object)sourceFile ?? DBNull.Value;

                    con.Open();
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                // Unique key violation
                return false;
            }
        }

        #endregion

        #region READ
        public async Task<StampImageSetQueueItem?> GetImageSetQueueItemAsync(
    string dictKey,
    CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dictKey))
                throw new ArgumentException("DictKey is required.", nameof(dictKey));

            const string sql = @"
SELECT
    DictKey,
    Status,
    Attempts,
    LastError,
    AprimoId,
    RelatedAssets,
    UpdatedAt
FROM ashley.dbo.StampImageSetsQueue
WHERE DictKey = @DictKey;
";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@DictKey", SqlDbType.NVarChar, 450)
            {
                Value = dictKey
            });

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                return null;

            return new StampImageSetQueueItem
            {
                DictKey = reader["DictKey"] as string,
                Status = Convert.ToInt32(reader["Status"]),
                Attempts = Convert.ToInt32(reader["Attempts"]),
                LastError = reader["LastError"] as string,
                AprimoId = reader["AprimoId"] as string,
                RelatedAssets = reader["RelatedAssets"] != DBNull.Value
                    ? Convert.ToInt32(reader["RelatedAssets"])
                    : null,

                UpdatedAt = reader["UpdatedAt"] != DBNull.Value
    ? reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("UpdatedAt"))
    : null
            };
        }

        public async Task<StampImageSetQueueItem?> GetImageSetQueueItemByAprimoIdAsync(
    string aprimoId,
    CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aprimoId))
                throw new ArgumentException("AprimoId is required.", nameof(aprimoId));

            const string sql = @"
SELECT
    DictKey,
    Status,
    Attempts,
    LastError,
    AprimoId,
    RelatedAssets,
    UpdatedAt
FROM ashley.dbo.StampImageSetsQueue
WHERE AprimoId = @AprimoId;
";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@AprimoId", SqlDbType.NVarChar, 100)
            {
                Value = aprimoId
            });

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                return null;

            return new StampImageSetQueueItem
            {
                DictKey = reader["DictKey"] as string,
                Status = Convert.ToInt32(reader["Status"]),
                Attempts = Convert.ToInt32(reader["Attempts"]),
                LastError = reader["LastError"] as string,
                AprimoId = reader["AprimoId"] as string,
                RelatedAssets = reader["RelatedAssets"] != DBNull.Value
                    ? Convert.ToInt32(reader["RelatedAssets"])
                    : null,

                UpdatedAt = reader["UpdatedAt"] != DBNull.Value
    ? reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("UpdatedAt"))
    : null
            };
        }

        public List<string> GetAllDictKeys()
        {
            var results = new List<string>();

            var sql = $"SELECT DictKey FROM {_tableName};";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                con.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            results.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return results;
        }

        public List<string> GetAllRecentDictKeys()
        {
            var results = new List<string>();

            var sql = $@"
SELECT DictKey
FROM {_tableName}
WHERE IngestedAt >
    DATEADD(HOUR, 9,
        CAST(
            CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'Eastern Standard Time' AS DATE)
        AS DATETIME2)
    )";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                con.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            results.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return results;
        }

        public List<string> GetAllSuccessDictKeys()
        {
            var results = new List<string>();

            var sql = $"SELECT DictKey FROM {_tableName} WHERE Status = 2 AND BatchId > {_batch};";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                con.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            results.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return results;
        }
        public string GetJsonBodyByDictKey(string dictKey)
        {
            if (string.IsNullOrWhiteSpace(dictKey))
                throw new ArgumentException("dictKey is required", nameof(dictKey));

            var sql =
                $"SELECT TOP (1) JsonBody FROM {_tableName} WHERE DictKey = @DictKey;";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@DictKey", SqlDbType.NVarChar, 450).Value = dictKey;

                con.Open();
                var result = cmd.ExecuteScalar();

                return result == null || result == DBNull.Value
                    ? null
                    : (string)result;
            }
        }

    public async Task<MappingHelperObject?> GetByAemAssetPathAsync(
        string aemAssetPath,
        CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aemAssetPath))
                throw new ArgumentException("AemAssetPath is required.", nameof(aemAssetPath));

            const string sql = @"
    SELECT
        AemAssetId,
        AemAssetPath,
        AemCreatedDate,
        AemAssetName,
        AzureAssetPath,
        AzureAssetName,
        ImageSets,
        ImageSetCount,
        AprimoId
    FROM ashley.dbo.MappingHelperObjectsFlat
    WHERE AemAssetPath = @AemAssetPath;
    ";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@AemAssetPath", SqlDbType.NVarChar, 1000)
            {
                Value = aemAssetPath
            });

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                return null;

            var imageSetsString = reader["ImageSets"] as string;

            return new MappingHelperObject
            {
                AemAssetId = reader["AemAssetId"] as string,
                AemAssetPath = reader["AemAssetPath"] as string,
                AemCreatedDate = reader["AemCreatedDate"] as string,
                AemAssetName = reader["AemAssetName"] as string,
                AzureAssetPath = reader["AzureAssetPath"] as string,
                AzureAssetName = reader["AzureAssetName"] as string,
                ImageSets = string.IsNullOrWhiteSpace(imageSetsString)
                    ? new List<string>()
                    : new List<string>(imageSetsString.Split(',', StringSplitOptions.RemoveEmptyEntries)),
                ImageSetCount = reader["ImageSetCount"] != DBNull.Value
                    ? Convert.ToInt32(reader["ImageSetCount"])
                    : 0,
                AprimoId = reader["AprimoId"] as string
            };
        }

        public async Task<MappingHelperObject?> GetByAprimoIdAsync(
            string aprimoId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aprimoId))
                throw new ArgumentException("AprimoId is required.", nameof(aprimoId));

            const string sql = @"
    SELECT
        AemAssetId,
        AemAssetPath,
        AemCreatedDate,
        AemAssetName,
        AzureAssetPath,
        AzureAssetName,
        ImageSets,
        ImageSetCount,
        AprimoId
    FROM ashley.dbo.MappingHelperObjectsFlat
    WHERE AprimoId = @AprimoId;
    ";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@AprimoId", SqlDbType.NVarChar, 1000)
            {
                Value = aprimoId
            });

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                return null;

            var imageSetsString = reader["ImageSets"] as string;

            return new MappingHelperObject
            {
                AemAssetId = reader["AemAssetId"] as string,
                AemAssetPath = reader["AemAssetPath"] as string,
                AemCreatedDate = reader["AemCreatedDate"] as string,
                AemAssetName = reader["AemAssetName"] as string,
                AzureAssetPath = reader["AzureAssetPath"] as string,
                AzureAssetName = reader["AzureAssetName"] as string,
                ImageSets = string.IsNullOrWhiteSpace(imageSetsString)
                    ? new List<string>()
                    : new List<string>(imageSetsString.Split(',', StringSplitOptions.RemoveEmptyEntries)),
                ImageSetCount = reader["ImageSetCount"] != DBNull.Value
                    ? Convert.ToInt32(reader["ImageSetCount"])
                    : 0,
                AprimoId = reader["AprimoId"] as string
            };
        }

        public async Task<MappingHelperObject?> GetByAemAssetIdAsync(
            string aemAssetId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aemAssetId))
                throw new ArgumentException("AEM ASSET ID is required.", nameof(aemAssetId));

            const string sql = @"
    SELECT
        AemAssetId,
        AemAssetPath,
        AemCreatedDate,
        AemAssetName,
        AzureAssetPath,
        AzureAssetName,
        ImageSets,
        ImageSetCount,
        AprimoId
    FROM ashley.dbo.MappingHelperObjectsFlat
    WHERE AemAssetId = @AemAssetId;
    ";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@AemAssetId", SqlDbType.NVarChar, 1000)
            {
                Value = aemAssetId
            });

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                return null;

            var imageSetsString = reader["ImageSets"] as string;

            return new MappingHelperObject
            {
                AemAssetId = reader["AemAssetId"] as string,
                AemAssetPath = reader["AemAssetPath"] as string,
                AemCreatedDate = reader["AemCreatedDate"] as string,
                AemAssetName = reader["AemAssetName"] as string,
                AzureAssetPath = reader["AzureAssetPath"] as string,
                AzureAssetName = reader["AzureAssetName"] as string,
                ImageSets = string.IsNullOrWhiteSpace(imageSetsString)
                    ? new List<string>()
                    : new List<string>(imageSetsString.Split(',', StringSplitOptions.RemoveEmptyEntries)),
                ImageSetCount = reader["ImageSetCount"] != DBNull.Value
                    ? Convert.ToInt32(reader["ImageSetCount"])
                    : 0,
                AprimoId = reader["AprimoId"] as string
            };
        }

        public async Task<List<MappingHelperObject>> GetWhereAprimoIdIsNullAsync(
    CancellationToken ct = default)
        {
            const string sql = @"
SELECT
    AemAssetId,
    AemAssetPath,
    AemCreatedDate,
    AemAssetName,
    AzureAssetPath,
    AzureAssetName,
    ImageSets,
    ImageSetCount,
    AprimoId
FROM ashley.dbo.MappingHelperObjectsFlat
WHERE AprimoId IS NULL;
";

            var results = new List<MappingHelperObject>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var imageSetsString = reader["ImageSets"] as string;

                results.Add(new MappingHelperObject
                {
                    AemAssetId = reader["AemAssetId"] as string,
                    AemAssetPath = reader["AemAssetPath"] as string,
                    AemCreatedDate = reader["AemCreatedDate"] as string,
                    AemAssetName = reader["AemAssetName"] as string,
                    AzureAssetPath = reader["AzureAssetPath"] as string,
                    AzureAssetName = reader["AzureAssetName"] as string,
                    ImageSets = string.IsNullOrWhiteSpace(imageSetsString)
                        ? new List<string>()
                        : new List<string>(imageSetsString.Split(',', StringSplitOptions.RemoveEmptyEntries)),
                    ImageSetCount = reader["ImageSetCount"] != DBNull.Value
                        ? Convert.ToInt32(reader["ImageSetCount"])
                        : 0,
                    AprimoId = null
                });
            }

            return results;
        }

        public async Task<List<MappingHelperObject>> GetAllFlatsFromTableOrViewAsync(
    CancellationToken ct = default)
        {
            const string sql = @"
SELECT
    AemAssetId,
    AemAssetPath,
    AemCreatedDate,
    AemAssetName,
    AzureAssetPath,
    AzureAssetName,
    ImageSets,
    ImageSetCount,
    AprimoId
FROM ashley.dbo.view_FlatsWithDuplicates;
";

            var results = new List<MappingHelperObject>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var imageSetsString = reader["ImageSets"] as string;

                results.Add(new MappingHelperObject
                {
                    AemAssetId = reader["AemAssetId"] as string,
                    AemAssetPath = reader["AemAssetPath"] as string,
                    AemCreatedDate = reader["AemCreatedDate"] as string,
                    AemAssetName = reader["AemAssetName"] as string,
                    AzureAssetPath = reader["AzureAssetPath"] as string,
                    AzureAssetName = reader["AzureAssetName"] as string,
                    ImageSets = string.IsNullOrWhiteSpace(imageSetsString)
                        ? new List<string>()
                        : new List<string>(imageSetsString.Split(',', StringSplitOptions.RemoveEmptyEntries)),
                    ImageSetCount = reader["ImageSetCount"] != DBNull.Value
                        ? Convert.ToInt32(reader["ImageSetCount"])
                        : 0,
                    AprimoId = null
                });
            }

            return results;
        }

        public async Task<List<MappingHelperObject>> GetAllFlatsFromParenthesesViewAsync(
CancellationToken ct = default)
        {
            const string sql = @"
SELECT
    AemAssetId,
    AemAssetPath,
    AemCreatedDate,
    AemAssetName,
    AzureAssetPath,
    AzureAssetName,
    ImageSets,
    ImageSetCount,
    AprimoId
FROM ashley.dbo.view_allAssetsWithParentheses;
";

            var results = new List<MappingHelperObject>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var imageSetsString = reader["ImageSets"] as string;

                results.Add(new MappingHelperObject
                {
                    AemAssetId = reader["AemAssetId"] as string,
                    AemAssetPath = reader["AemAssetPath"] as string,
                    AemCreatedDate = reader["AemCreatedDate"] as string,
                    AemAssetName = reader["AemAssetName"] as string,
                    AzureAssetPath = reader["AzureAssetPath"] as string,
                    AzureAssetName = reader["AzureAssetName"] as string,
                    ImageSets = string.IsNullOrWhiteSpace(imageSetsString)
                        ? new List<string>()
                        : new List<string>(imageSetsString.Split(',', StringSplitOptions.RemoveEmptyEntries)),
                    ImageSetCount = reader["ImageSetCount"] != DBNull.Value
                        ? Convert.ToInt32(reader["ImageSetCount"])
                        : 0,
                    AprimoId = reader["AprimoId"] as string,
                });
            }

            return results;
        }

        public bool Exists(string dictKey)
        {
            var sql =
                $"SELECT 1 FROM {_tableName} WHERE DictKey = @DictKey;";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@DictKey", SqlDbType.NVarChar, 450).Value = dictKey;

                con.Open();
                var result = cmd.ExecuteScalar();
                return result != null;
            }
        }

        #endregion

        #region UPDATE

public async Task ResetImageSetQueueAsync(
    List<string> dictKeys,
    CancellationToken ct = default)
    {
        if (dictKeys == null || dictKeys.Count == 0)
            return;

        var table = new DataTable();
        table.Columns.Add("DictKey", typeof(string));

        foreach (var key in dictKeys)
            table.Rows.Add(key);

        const string sql = @"
UPDATE q
SET
    q.Status = 0,
    q.Attempts = 0,
    q.LastError = NULL,
    q.AprimoId = NULL,
    q.RelatedAssets = NULL,
    q.UpdatedAt = SYSDATETIMEOFFSET()
FROM ashley.dbo.StampImageSetsQueue q
INNER JOIN @Keys k
    ON q.DictKey = k.DictKey;
";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand(sql, conn);
        cmd.CommandType = CommandType.Text;

        var param = cmd.Parameters.AddWithValue("@Keys", table);
        param.SqlDbType = SqlDbType.Structured;
        param.TypeName = "dbo.ImageSetKeyList";

        await cmd.ExecuteNonQueryAsync(ct);
    }


    public async Task<bool> UpdateAprimoIdAsync(
    string aemAssetId,
    string aprimoId,
    CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aemAssetId))
                throw new ArgumentException("AemAssetId is required.", nameof(aemAssetId));

            const string sql = @"
UPDATE ashley.dbo.MappingHelperObjectsFlat
SET
    AprimoId = @AprimoId,
    UpdatedAt = SYSDATETIMEOFFSET()
WHERE AemAssetId = @AemAssetId;
";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@AemAssetId", SqlDbType.NVarChar, 100)
            {
                Value = aemAssetId
            });

            cmd.Parameters.Add(new SqlParameter("@AprimoId", SqlDbType.NVarChar, 100)
            {
                Value = (object?)aprimoId ?? DBNull.Value
            });

            var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);

            return rowsAffected > 0;
        }

        public async Task<bool> UpdateImageSetsAsync(
            string aemAssetId,
            List<string> imageSets,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(aemAssetId))
                throw new ArgumentException("AemAssetId is required.", nameof(aemAssetId));

            string imageSetsString = null;
            int imageSetCount = 0;

            if (imageSets != null && imageSets.Count > 0)
            {
                imageSetsString = string.Join(",", imageSets);
                imageSetCount = imageSets.Count;
            }

            const string sql = @"
UPDATE ashley.dbo.MappingHelperObjectsFlat
SET
    ImageSets = @ImageSets,
    ImageSetCount = @ImageSetCount,
    UpdatedAt = SYSDATETIMEOFFSET()
WHERE AemAssetId = @AemAssetId;
";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@AemAssetId", SqlDbType.NVarChar, 100)
            {
                Value = aemAssetId
            });

            cmd.Parameters.Add(new SqlParameter("@ImageSets", SqlDbType.NVarChar, -1) // NVARCHAR(MAX)
            {
                Value = (object?)imageSetsString ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@ImageSetCount", SqlDbType.Int)
            {
                Value = imageSetCount
            });

            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }


        public async Task BulkInsertAsync(
    List<MappingHelperObject> items,
    CancellationToken ct = default)
    {
        if (items == null || items.Count == 0)
            return;

        var table = new DataTable();

        table.Columns.Add("AemAssetId", typeof(string));
        table.Columns.Add("AemAssetPath", typeof(string));
        table.Columns.Add("AemCreatedDate", typeof(string));
        table.Columns.Add("AemAssetName", typeof(string));
        table.Columns.Add("AzureAssetPath", typeof(string));
        table.Columns.Add("AzureAssetName", typeof(string));
        table.Columns.Add("ImageSets", typeof(string));
        table.Columns.Add("ImageSetCount", typeof(int));
        table.Columns.Add("AprimoId", typeof(string));

        foreach (var item in items)
        {
            table.Rows.Add(
                item.AemAssetId,
                item.AemAssetPath,
                item.AemCreatedDate,
                item.AemAssetName,
                item.AzureAssetPath,
                item.AzureAssetName,
                item.ImageSets != null && item.ImageSets.Count > 0
                    ? string.Join(",", item.ImageSets)
                    : null,
                item.ImageSetCount,
                item.AprimoId
            );
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        using var bulkCopy = new SqlBulkCopy(conn)
        {
            DestinationTableName = "ashley.dbo.MappingHelperObjectsFlat",
            BatchSize = 5000,
            BulkCopyTimeout = 0
        };

        bulkCopy.ColumnMappings.Add("AemAssetId", "AemAssetId");
        bulkCopy.ColumnMappings.Add("AemAssetPath", "AemAssetPath");
        bulkCopy.ColumnMappings.Add("AemCreatedDate", "AemCreatedDate");
        bulkCopy.ColumnMappings.Add("AemAssetName", "AemAssetName");
        bulkCopy.ColumnMappings.Add("AzureAssetPath", "AzureAssetPath");
        bulkCopy.ColumnMappings.Add("AzureAssetName", "AzureAssetName");
        bulkCopy.ColumnMappings.Add("ImageSets", "ImageSets");
        bulkCopy.ColumnMappings.Add("ImageSetCount", "ImageSetCount");
        bulkCopy.ColumnMappings.Add("AprimoId", "AprimoId");

        await bulkCopy.WriteToServerAsync(table, ct);
    }


    public bool UpdateJsonBody(string dictKey, string jsonBody, string sourceFile = null)
        {
            var sql =
                $"UPDATE {_tableName} " +
                $"SET JsonBody = @JsonBody, " +
                $"    SourceFile = COALESCE(@SourceFile, SourceFile), " +
                $"    IngestedAt = SYSDATETIMEOFFSET() " +
                $"WHERE DictKey = @DictKey;";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@DictKey", SqlDbType.NVarChar, 450).Value = dictKey;
                cmd.Parameters.Add("@JsonBody", SqlDbType.NVarChar).Value = jsonBody ?? string.Empty;
                cmd.Parameters.Add("@SourceFile", SqlDbType.NVarChar, 260).Value =
                    (object)sourceFile ?? DBNull.Value;

                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public void UpsertJsonBody(string dictKey, string jsonBody, string sourceFile = null)
        {
            var sql =
                $"UPDATE {_tableName} " +
                $"SET JsonBody = @JsonBody, " +
                $"    SourceFile = COALESCE(@SourceFile, SourceFile), " +
                $"    IngestedAt = SYSDATETIMEOFFSET() " +
                $"WHERE DictKey = @DictKey; " +
                $"IF @@ROWCOUNT = 0 " +
                $"INSERT INTO {_tableName} (DictKey, JsonBody, SourceFile, IngestedAt) " +
                $"VALUES (@DictKey, @JsonBody, @SourceFile, SYSDATETIMEOFFSET());";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@DictKey", SqlDbType.NVarChar, 450).Value = dictKey;
                cmd.Parameters.Add("@JsonBody", SqlDbType.NVarChar).Value = jsonBody ?? string.Empty;
                cmd.Parameters.Add("@SourceFile", SqlDbType.NVarChar, 260).Value =
                    (object)sourceFile ?? DBNull.Value;

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }


        public async Task UpsertMHOFlatAsync(
            MappingHelperObject item,
            CancellationToken ct = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrWhiteSpace(item.AemAssetId))
                throw new ArgumentException("AemAssetId is required.", nameof(item.AemAssetId));

            var imageSetsString = item.ImageSets != null && item.ImageSets.Count > 0
                ? string.Join(",", item.ImageSets)
                : null;

            const string sql = @"
UPDATE ashley.dbo.MappingHelperObjectsFlat
SET
    AemAssetPath = @AemAssetPath,
    AemCreatedDate = @AemCreatedDate,
    AemAssetName = @AemAssetName,
    AzureAssetPath = @AzureAssetPath,
    AzureAssetName = @AzureAssetName,
    ImageSets = @ImageSets,
    ImageSetCount = @ImageSetCount,
    AprimoId = @AprimoId,
    UpdatedAt = SYSDATETIMEOFFSET()
WHERE AemAssetId = @AemAssetId;

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO ashley.dbo.MappingHelperObjectsFlat
    (
        AemAssetId,
        AemAssetPath,
        AemCreatedDate,
        AemAssetName,
        AzureAssetPath,
        AzureAssetName,
        ImageSets,
        ImageSetCount,
        AprimoId,
        CreatedAt,
        UpdatedAt
    )
    VALUES
    (
        @AemAssetId,
        @AemAssetPath,
        @AemCreatedDate,
        @AemAssetName,
        @AzureAssetPath,
        @AzureAssetName,
        @ImageSets,
        @ImageSetCount,
        @AprimoId,
        SYSDATETIMEOFFSET(),
        SYSDATETIMEOFFSET()
    );
END
";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@AemAssetId", SqlDbType.NVarChar, 100)
            {
                Value = item.AemAssetId
            });

            cmd.Parameters.Add(new SqlParameter("@AemAssetPath", SqlDbType.NVarChar, 1000)
            {
                Value = (object?)item.AemAssetPath ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@AemCreatedDate", SqlDbType.NVarChar, 50)
            {
                Value = (object?)item.AemCreatedDate ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@AemAssetName", SqlDbType.NVarChar, 500)
            {
                Value = (object?)item.AemAssetName ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@AzureAssetPath", SqlDbType.NVarChar, 1000)
            {
                Value = (object?)item.AzureAssetPath ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@AzureAssetName", SqlDbType.NVarChar, 500)
            {
                Value = (object?)item.AzureAssetName ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@ImageSets", SqlDbType.NVarChar, -1)
            {
                Value = (object?)imageSetsString ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@ImageSetCount", SqlDbType.Int)
            {
                Value = item.ImageSetCount
            });

            cmd.Parameters.Add(new SqlParameter("@AprimoId", SqlDbType.NVarChar, 100)
            {
                Value = (object?)item.AprimoId ?? DBNull.Value
            });

            await cmd.ExecuteNonQueryAsync(ct);
        }
        #endregion

        #region DELETE

        public bool Delete(string dictKey)
        {
            var sql = $"DELETE FROM {_tableName} WHERE DictKey = @DictKey;";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@DictKey", SqlDbType.NVarChar, 450).Value = dictKey;

                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        #endregion
    }

    public class StampImageSetQueueItem
    {
        public string DictKey { get; set; }
        public int Status { get; set; }
        public int Attempts { get; set; }
        public string LastError { get; set; }
        public string AprimoId { get; set; }
        public int? RelatedAssets { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

}
