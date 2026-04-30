using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;


namespace Migration.Manifest.Sqlite.Services
{

    public sealed class SqliteFixedWindowRateLimiter
    {
        private readonly string _dbPath;

        public SqliteFixedWindowRateLimiter(string dbPath)
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

        private void Initialize()
        {
            using var conn = OpenReadWrite();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;

CREATE TABLE IF NOT EXISTS rate_window (
  host   TEXT NOT NULL,
  ts_sec INTEGER NOT NULL,
  count  INTEGER NOT NULL,
  PRIMARY KEY (host, ts_sec)
);

CREATE INDEX IF NOT EXISTS idx_rate_window_host_ts
  ON rate_window(host, ts_sec);
";
            cmd.ExecuteNonQuery();
        }

        public async Task AcquireAsync(string host, int maxPerSecond, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(host)) host = "unknown";
            if (maxPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(maxPerSecond));

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var tsSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                using var conn = OpenReadWrite();

                // Take write lock early so multiple processes don't race.
                using (var begin = conn.CreateCommand())
                {
                    begin.CommandText = "BEGIN IMMEDIATE;";
                    begin.ExecuteNonQuery();
                }

                try
                {
                    long current = 0;

                    using (var read = conn.CreateCommand())
                    {
                        read.CommandText = @"
SELECT count FROM rate_window
WHERE host = $host AND ts_sec = $ts
LIMIT 1;";
                        read.Parameters.AddWithValue("$host", host);
                        read.Parameters.AddWithValue("$ts", tsSec);

                        var scalar = read.ExecuteScalar();
                        if (scalar != null && scalar != DBNull.Value)
                            current = Convert.ToInt64(scalar);
                    }

                    if (current < maxPerSecond)
                    {
                        using var upsert = conn.CreateCommand();
                        upsert.CommandText = @"
INSERT INTO rate_window(host, ts_sec, count)
VALUES ($host, $ts, 1)
ON CONFLICT(host, ts_sec)
DO UPDATE SET count = count + 1;";
                        upsert.Parameters.AddWithValue("$host", host);
                        upsert.Parameters.AddWithValue("$ts", tsSec);
                        upsert.ExecuteNonQuery();

                        using var commit = conn.CreateCommand();
                        commit.CommandText = "COMMIT;";
                        commit.ExecuteNonQuery();
                        return;
                    }

                    using (var rollback = conn.CreateCommand())
                    {
                        rollback.CommandText = "ROLLBACK;";
                        rollback.ExecuteNonQuery();
                    }
                }
                catch
                {
                    try
                    {
                        using var rollback = conn.CreateCommand();
                        rollback.CommandText = "ROLLBACK;";
                        rollback.ExecuteNonQuery();
                    }
                    catch { /* ignore */ }

                    throw;
                }

                // Wait until next second boundary
                var next = DateTimeOffset.FromUnixTimeSeconds(tsSec + 1);
                var delay = next - DateTimeOffset.UtcNow;
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromMilliseconds(5);
                delay += TimeSpan.FromMilliseconds(5);

                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }

}
