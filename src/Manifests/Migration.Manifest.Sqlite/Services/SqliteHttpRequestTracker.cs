using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Migration.Manifest.Sqlite.Services
{


    public sealed class SqliteHttpRequestTracker
    {
        private readonly string _dbPath;

        public SqliteHttpRequestTracker(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
            Initialize();
        }

        private SqliteConnection OpenReadWrite()
        {
            var cs = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            var conn = new SqliteConnection(cs);
            conn.Open();
            return conn;
        }

        private SqliteConnection OpenReadOnly()
        {
            var cs = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            var conn = new SqliteConnection(cs);
            conn.Open();
            return conn;
        }

        private void Initialize()
        {
            using var conn = OpenReadWrite();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;

CREATE TABLE IF NOT EXISTS http_req_counts (
  ts_sec INTEGER NOT NULL,
  host   TEXT NOT NULL,
  path   TEXT NOT NULL,
  method TEXT NOT NULL,
  status INTEGER NOT NULL,
  count  INTEGER NOT NULL,
  PRIMARY KEY (ts_sec, host, path, method, status)
);

CREATE INDEX IF NOT EXISTS idx_http_req_counts_host_ts
  ON http_req_counts(host, ts_sec);

CREATE INDEX IF NOT EXISTS idx_http_req_counts_host_path_ts
  ON http_req_counts(host, path, ts_sec);
";
            cmd.ExecuteNonQuery();
        }

        public void Record(long tsSecUtc, string host, string path, string method, int status)
        {
            host ??= "unknown";
            path ??= "";
            method ??= "";

            using var conn = OpenReadWrite();
            using var tx = conn.BeginTransaction();

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
INSERT INTO http_req_counts(ts_sec, host, path, method, status, count)
VALUES ($ts, $host, $path, $method, $status, 1)
ON CONFLICT(ts_sec, host, path, method, status)
DO UPDATE SET count = count + 1;
";
            cmd.Parameters.AddWithValue("$ts", tsSecUtc);
            cmd.Parameters.AddWithValue("$host", host);
            cmd.Parameters.AddWithValue("$path", path);
            cmd.Parameters.AddWithValue("$method", method);
            cmd.Parameters.AddWithValue("$status", status);

            cmd.ExecuteNonQuery();
            tx.Commit();
        }

        public long GetTotal(string host, long fromSecUtc, long toSecUtc)
        {
            using var conn = OpenReadOnly();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT COALESCE(SUM(count), 0)
FROM http_req_counts
WHERE host = $host AND ts_sec BETWEEN $from AND $to;
";
            cmd.Parameters.AddWithValue("$host", host);
            cmd.Parameters.AddWithValue("$from", fromSecUtc);
            cmd.Parameters.AddWithValue("$to", toSecUtc);

            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0);
        }

        public IReadOnlyDictionary<int, long> GetStatusCounts(string host, long fromSecUtc, long toSecUtc)
        {
            var result = new Dictionary<int, long>();

            using var conn = OpenReadOnly();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT status, COALESCE(SUM(count), 0)
FROM http_req_counts
WHERE host = $host AND ts_sec BETWEEN $from AND $to
GROUP BY status
ORDER BY status;
";
            cmd.Parameters.AddWithValue("$host", host);
            cmd.Parameters.AddWithValue("$from", fromSecUtc);
            cmd.Parameters.AddWithValue("$to", toSecUtc);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result[reader.GetInt32(0)] = reader.GetInt64(1);
            }

            return result;
        }

        public long GetTotalForPath(string host, string path, long fromSecUtc, long toSecUtc)
        {
            using var conn = OpenReadOnly();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT COALESCE(SUM(count), 0)
FROM http_req_counts
WHERE host = $host AND path = $path AND ts_sec BETWEEN $from AND $to;
";
            cmd.Parameters.AddWithValue("$host", host);
            cmd.Parameters.AddWithValue("$path", path ?? "");
            cmd.Parameters.AddWithValue("$from", fromSecUtc);
            cmd.Parameters.AddWithValue("$to", toSecUtc);

            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0);
        }
    }

}
