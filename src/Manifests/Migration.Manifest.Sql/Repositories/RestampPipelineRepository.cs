using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Migration.Manifest.Sql.Repositories
{

    public sealed class RestampPipelineRepository
    {
        private readonly string _cs;

        // Tables
        private const string RestampQueue = "ashley.dbo.RestampQueue";
        private const string StampImageSetsQueue = "ashley.dbo.StampImageSetsQueue";

        private const string MappingHelperObjects = "ashley.dbo.MappingHelperObjects";
        private const string AssetMetadata = "ashley.dbo.AssetMetadata";

        private const string ImageSets = "ashley.dbo.ImageSets";
        private const string ImageSetsRelations = "ashley.dbo.ImageSetsRelations";

        // TVPs
        private const string AssetKeysTvpType = "dbo.AssetKeyList";
        private const string ImageSetKeysTvpType = "dbo.ImageSetKeyList";


        public RestampPipelineRepository(string connectionString) => _cs = connectionString;

        // ---------------------------
        // InitializeQueueAsync
        // ---------------------------
        public async Task InitializeQueueAsync(int batchSize = 2000, CancellationToken ct = default)
        {
            var sql = $@"
                TRUNCATE TABLE {RestampQueue};

                DECLARE @BatchSize int = @pBatchSize;

                INSERT INTO {RestampQueue} (DictKey, BatchId, Status, Attempts, LastError, UpdatedAt)
                SELECT
                    mho.DictKey,
                    (ROW_NUMBER() OVER (ORDER BY mho.DictKey) - 1) / @BatchSize + 1 AS BatchId,
                    CAST(0 AS tinyint),
                    0,
                    NULL,
                    SYSDATETIMEOFFSET()
                FROM {MappingHelperObjects} mho;";

            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@pBatchSize", SqlDbType.Int).Value = batchSize;
                await con.OpenAsync(ct).ConfigureAwait(false);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
        }

        // ---------------------------
        // ClaimNextBatchAsync (Assets)
        // ---------------------------
        public async Task<List<string>> ClaimNextBatchAsync(int batchSize = 2000, CancellationToken ct = default)
        {
            var sql = $@"
                ;WITH cte AS
                (
                    SELECT TOP (@Take) DictKey
                    FROM {RestampQueue} WITH (READPAST, UPDLOCK, ROWLOCK)
                    WHERE Status = 0
                    ORDER BY BatchId, DictKey
                )
                UPDATE q
                SET Status = 1,
                    Attempts = Attempts + 1,
                    UpdatedAt = SYSDATETIMEOFFSET()
                OUTPUT inserted.DictKey
                FROM {RestampQueue} q
                JOIN cte ON cte.DictKey = q.DictKey;";

            var keys = new List<string>(batchSize);

            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@Take", SqlDbType.Int).Value = batchSize;
                await con.OpenAsync(ct).ConfigureAwait(false);

                using (var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
                {
                    while (await rdr.ReadAsync(ct).ConfigureAwait(false))
                        keys.Add(rdr.GetString(0));
                }
            }

            return keys;
        }

        // ---------------------------
        // FetchJsonForKeysAsync (Assets) - JOIN MappingHelperObjects + AssetMetadata
        // ---------------------------
        public async Task<List<AssetStampRow>> FetchJsonForKeysAsync(IEnumerable<string> dictKeys, CancellationToken ct = default)
        {
            var keys = (dictKeys ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            if (keys.Count == 0) return new List<AssetStampRow>();

            var tvp = new DataTable();
            tvp.Columns.Add("DictKey", typeof(string));
            foreach (var k in keys) tvp.Rows.Add(k);

            var sql = $@"
                SELECT
                    k.DictKey,
                    mho.JsonBody AS MappingHelperJson,
                    am.JsonBody  AS AssetMetadataJson
                FROM @Keys k
                JOIN {MappingHelperObjects} mho ON mho.DictKey = k.DictKey
                JOIN {AssetMetadata}       am  ON am.DictKey  = k.DictKey;";

            var results = new List<AssetStampRow>(keys.Count);

            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                var p = cmd.Parameters.AddWithValue("@Keys", tvp);
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = AssetKeysTvpType;

                await con.OpenAsync(ct).ConfigureAwait(false);
                using (var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
                {
                    while (await rdr.ReadAsync(ct).ConfigureAwait(false))
                    {
                        results.Add(new AssetStampRow
                        {
                            DictKey = rdr.GetString(0),
                            MappingHelperJson = rdr.IsDBNull(1) ? null : rdr.GetString(1),
                            AssetMetadataJson = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                        });
                    }
                }
            }

            return results;
        }

        // ---------------------------
        // MarkDoneAsync / MarkFailedAsync (Assets)
        // ---------------------------
        public Task MarkDoneAsync(IEnumerable<string> dictKeys, CancellationToken ct = default)
            => UpdateQueueStatusAsync(RestampQueue, dictKeys, status: 2, lastError: null, ct);

        public Task MarkFailedAsync(IEnumerable<string> dictKeys, string error, CancellationToken ct = default)
            => UpdateQueueStatusAsync(RestampQueue, dictKeys, status: 3, lastError: Trunc(error), ct);

        // ---------------------------
        // ImageSets phase (after assets done)
        // ---------------------------
        public async Task<List<string>> ClaimNextImageSetBatchAsync(int batchSize = 2000, CancellationToken ct = default)
        {
            var sql = $@"
                ;WITH cte AS
                (
                    SELECT TOP (@Take) DictKey
                    FROM {StampImageSetsQueue} WITH (READPAST, UPDLOCK, ROWLOCK)
                    WHERE Status = 0
                    ORDER BY BatchId, DictKey
                )
                UPDATE q
                SET Status = 1,
                    Attempts = Attempts + 1,
                    UpdatedAt = SYSDATETIMEOFFSET()
                OUTPUT inserted.DictKey
                FROM {StampImageSetsQueue} q
                JOIN cte ON cte.DictKey = q.DictKey;";

            var keys = new List<string>(batchSize);

            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@Take", SqlDbType.Int).Value = batchSize;
                await con.OpenAsync(ct).ConfigureAwait(false);

                using (var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
                {
                    while (await rdr.ReadAsync(ct).ConfigureAwait(false))
                        keys.Add(rdr.GetString(0));
                }
            }

            return keys;
        }

        public async Task<List<ImageSetStampRow>> FetchImageSetJsonForKeysAsync(IEnumerable<string> dictKeys, CancellationToken ct = default)
        {
            var keys = (dictKeys ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            if (keys.Count == 0) return new List<ImageSetStampRow>();

            var tvp = new DataTable();
            tvp.Columns.Add("DictKey", typeof(string));
            foreach (var k in keys) tvp.Rows.Add(k);

            var sql = $@"
                SELECT
                    k.DictKey,
                    s.JsonBody AS ImageSetJson,
                    r.JsonBody AS ImageSetRelationsJson
                FROM @Keys k
                JOIN {ImageSets} s
                  ON s.DictKey COLLATE Latin1_General_100_CS_AS = k.DictKey COLLATE Latin1_General_100_CS_AS
                JOIN {ImageSetsRelations} r
                  ON r.DictKey COLLATE Latin1_General_100_CS_AS = k.DictKey COLLATE Latin1_General_100_CS_AS;";

            var results = new List<ImageSetStampRow>(keys.Count);

            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                var p = cmd.Parameters.AddWithValue("@Keys", tvp);
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = ImageSetKeysTvpType;

                await con.OpenAsync(ct).ConfigureAwait(false);
                using (var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
                {
                    while (await rdr.ReadAsync(ct).ConfigureAwait(false))
                    {
                        results.Add(new ImageSetStampRow
                        {
                            DictKey = rdr.GetString(0),
                            ImageSetJson = rdr.IsDBNull(1) ? null : rdr.GetString(1),
                            ImageSetRelationsJson = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                        });
                    }
                }
            }

            return results;
        }

        public Task MarkImageSetsDoneAsync(IEnumerable<string> dictKeys, CancellationToken ct = default)
            => UpdateQueueStatusAsync(StampImageSetsQueue, dictKeys, status: 2, lastError: null, ct);

        public Task MarkImageSetsFailedAsync(IEnumerable<string> dictKeys, string error, CancellationToken ct = default)
            => UpdateQueueStatusAsync(StampImageSetsQueue, dictKeys, status: 3, lastError: Trunc(error), ct);

        // ---------------------------
        // Shared queue status updater (TVP)
        // ---------------------------
        private async Task UpdateQueueStatusAsync(string queueTable, IEnumerable<string> dictKeys, byte status, string lastError, CancellationToken ct)
        {
            var keys = (dictKeys ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            if (keys.Count == 0) return;

            var tvp = new DataTable();
            tvp.Columns.Add("DictKey", typeof(string));
            foreach (var k in keys) tvp.Rows.Add(k);

            // Choose TVP based on queue table (assets CI vs imagesets CS)
            var tvpType = (queueTable.EndsWith("StampImageSetsQueue", StringComparison.OrdinalIgnoreCase))
                ? ImageSetKeysTvpType
                : AssetKeysTvpType;

            var sql = $@"
                UPDATE q
                SET Status = @Status,
                    LastError = @LastError,
                    UpdatedAt = SYSDATETIMEOFFSET()
                FROM {queueTable} q
                JOIN @Keys k
                  ON q.DictKey = k.DictKey;";

            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@Status", SqlDbType.TinyInt).Value = status;
                cmd.Parameters.Add("@LastError", SqlDbType.NVarChar, 2000).Value = (object)lastError ?? DBNull.Value;

                var p = cmd.Parameters.AddWithValue("@Keys", tvp);
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = tvpType;

                await con.OpenAsync(ct).ConfigureAwait(false);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
        }

        public async Task UpdateImageSetQueueDetailsAsync(
            string dictKey,
            string aprimoId,
            int relatedAssets,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dictKey))
                throw new ArgumentException("dictKey is required.", nameof(dictKey));

            const string sql = @"
UPDATE ashley.dbo.StampImageSetsQueue
SET
    AprimoId = @AprimoId,
    RelatedAssets = @RelatedAssets,
    UpdatedAt = SYSDATETIMEOFFSET()
WHERE DictKey = @DictKey;
";

            await using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(new SqlParameter("@DictKey", SqlDbType.NVarChar, 450)
            {
                Value = dictKey
            });

            cmd.Parameters.Add(new SqlParameter("@AprimoId", SqlDbType.NVarChar, 100)
            {
                Value = (object?)aprimoId ?? DBNull.Value
            });

            cmd.Parameters.Add(new SqlParameter("@RelatedAssets", SqlDbType.Int)
            {
                Value = relatedAssets
            });

            await cmd.ExecuteNonQueryAsync(ct);
        }


        private static string Trunc(string s) => string.IsNullOrEmpty(s) ? s : (s.Length <= 2000 ? s : s.Substring(0, 2000));
    }

    public sealed class AssetStampRow
    {
        public string DictKey { get; set; }
        public string MappingHelperJson { get; set; }
        public string AssetMetadataJson { get; set; }
    }

    public sealed class ImageSetStampRow
    {
        public string DictKey { get; set; }
        public string ImageSetJson { get; set; }
        public string ImageSetRelationsJson { get; set; }
    }

}
