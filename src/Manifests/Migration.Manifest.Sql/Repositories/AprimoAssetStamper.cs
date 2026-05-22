using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Manifest.Sql.Repositories
{
    public static class AprimoAssetStamper
    {
        // Status codes assumed by queue tables:
        // 0=Pending, 1=InProgress, 2=Done, 3=Failed

        #region Options + Progress Models

        public sealed class StamperOptions
        {
            public int BatchSize { get; set; } = 2000;
            public int? MaxBatches { get; set; } = null;
            public int MaxDegreeOfParallelism { get; set; } = 12;

            public int MaxRetries { get; set; } = 5;
            public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(1);
            public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(30);

            public Action<ProgressInfo> Progress { get; set; } = null;
            public TimeSpan ProgressInterval { get; set; } = TimeSpan.FromSeconds(10);

            public Func<Exception, bool> ShouldRetry { get; set; } = null;
            public Action<RetryInfo> OnRetry { get; set; } = null;
        }

        public sealed class ProgressInfo
        {
            public string Phase { get; set; } // "Assets" or "ImageSets"
            public int BatchesCompleted { get; set; }
            public long TotalClaimed { get; set; }
            public long TotalDone { get; set; }
            public long TotalFailed { get; set; }
            public TimeSpan Elapsed { get; set; }
            public double ItemsPerSecond { get; set; }
            public TimeSpan? EstimatedTimeRemaining { get; set; }
        }

        public sealed class RetryInfo
        {
            public string DictKey { get; set; }
            public int Attempt { get; set; }
            public TimeSpan Delay { get; set; }
            public Exception Exception { get; set; }
        }

        public sealed class StampRunSummary
        {
            public string Phase { get; set; }
            public int BatchesCompleted { get; set; }
            public long TotalClaimed { get; set; }
            public long TotalDone { get; set; }
            public long TotalFailed { get; set; }
            public TimeSpan Elapsed { get; set; }
        }

        /// <summary>
        /// Explicit per-item success/failure result so you can mark failed even if you catch exceptions.
        /// Retryable=true means the stamper will throw to trigger retry/backoff (if retries remain).
        /// </summary>
        public sealed class ItemStampResult
        {
            public bool Success { get; init; }
            public bool Retryable { get; init; }
            public string ErrorMessage { get; init; }

            public static ItemStampResult Ok() => new ItemStampResult { Success = true };
            public static ItemStampResult Fail(string message, bool retryable = false) =>
                new ItemStampResult { Success = false, Retryable = retryable, ErrorMessage = message };
        }

        public sealed class BatchStampResult
        {
            public List<string> DoneKeys { get; set; } = new List<string>();

            // Backward compatible: if you only provide FailedKeys + ErrorMessage, we'll apply ErrorMessage to all.
            public List<string> FailedKeys { get; set; } = new List<string>();
            public string ErrorMessage { get; set; }

            // NEW: if you provide per-key errors, we will write those into queue (grouped by message).
            public Dictionary<string, string> FailedKeyErrors { get; set; } = null;
        }

        #endregion

        #region Backward-compatible overloads (batch delegate)

        public static Task StampAllAprimoAssets(
            RestampPipelineRepository repo,
            Func<List<AssetStampRow>, CancellationToken, Task> stampBatchAsync,
            int batchSize = 2000,
            CancellationToken ct = default)
        {
            var opts = new StamperOptions { BatchSize = batchSize };
            return StampAllAprimoAssets(repo,
                async (rows, token) =>
                {
                    await stampBatchAsync(rows, token).ConfigureAwait(false);
                    return new BatchStampResult
                    {
                        DoneKeys = rows.Select(r => r.DictKey).ToList(),
                        FailedKeys = new List<string>()
                    };
                },
                opts,
                ct);
        }

        public static Task StampAllAprimoAssets(
            RestampPipelineRepository repo,
            Func<List<AssetStampRow>, CancellationToken, Task<BatchStampResult>> stampBatchAsync,
            int batchSize = 2000,
            int? maxBatches = null,
            CancellationToken ct = default)
        {
            var opts = new StamperOptions { BatchSize = batchSize, MaxBatches = maxBatches };
            return StampAllAprimoAssets(repo, stampBatchAsync, opts, ct);
        }

        #endregion

        #region Optimized Assets entrypoints

        public static async Task<StampRunSummary> StampAllAprimoAssets(
            RestampPipelineRepository repo,
            Func<List<AssetStampRow>, CancellationToken, Task<BatchStampResult>> stampBatchAsync,
            StamperOptions options,
            CancellationToken ct = default)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            if (stampBatchAsync == null) throw new ArgumentNullException(nameof(stampBatchAsync));
            options ??= new StamperOptions();
            if (options.BatchSize <= 0) throw new ArgumentOutOfRangeException(nameof(options.BatchSize));
            if (options.MaxDegreeOfParallelism <= 0) throw new ArgumentOutOfRangeException(nameof(options.MaxDegreeOfParallelism));

            var sw = Stopwatch.StartNew();
            var nextProgressAt = TimeSpan.Zero;

            long totalClaimed = 0;
            long totalDone = 0;
            long totalFailed = 0;
            int batchesCompleted = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var keys = await repo.ClaimNextBatchAsync(options.BatchSize, ct).ConfigureAwait(false);
                if (keys == null || keys.Count == 0)
                    break;

                totalClaimed += keys.Count;

                List<AssetStampRow> rows;
                try
                {
                    rows = await repo.FetchJsonForKeysAsync(keys, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await repo.MarkFailedAsync(keys, ex.Message, ct).ConfigureAwait(false);
                    totalFailed += keys.Count;
                    batchesCompleted++;
                    EmitProgressIfDue("Assets");
                    if (ReachedMaxBatches()) break;
                    continue;
                }

                var rowKeys = new HashSet<string>(rows.Select(r => r.DictKey), StringComparer.OrdinalIgnoreCase);
                var missing = keys.Where(k => !rowKeys.Contains(k)).ToList();
                if (missing.Count > 0)
                {
                    await repo.MarkFailedAsync(missing, "Missing MappingHelperObjects and/or AssetMetadata row(s) for DictKey.", ct).ConfigureAwait(false);
                    totalFailed += missing.Count;
                }

                if (rows.Count == 0)
                {
                    batchesCompleted++;
                    EmitProgressIfDue("Assets");
                    if (ReachedMaxBatches()) break;
                    continue;
                }

                BatchStampResult result;
                try
                {
                    result = await stampBatchAsync(rows, ct).ConfigureAwait(false) ?? new BatchStampResult();
                }
                catch (Exception ex)
                {
                    await repo.MarkFailedAsync(rows.Select(r => r.DictKey), ex.Message, ct).ConfigureAwait(false);
                    totalFailed += rows.Count;
                    batchesCompleted++;
                    EmitProgressIfDue("Assets");
                    if (ReachedMaxBatches()) break;
                    continue;
                }

                if (result.DoneKeys != null && result.DoneKeys.Count > 0)
                {
                    await repo.MarkDoneAsync(result.DoneKeys, ct).ConfigureAwait(false);
                    totalDone += result.DoneKeys.Count;
                }

                if (result.FailedKeyErrors != null && result.FailedKeyErrors.Count > 0)
                {
                    await MarkFailedGroupedAsync(
                        markFailed: (list, msg) => repo.MarkFailedAsync(list, msg, ct),
                        failedKeyErrors: result.FailedKeyErrors).ConfigureAwait(false);

                    totalFailed += result.FailedKeyErrors.Count;
                }
                else if (result.FailedKeys != null && result.FailedKeys.Count > 0)
                {
                    await repo.MarkFailedAsync(result.FailedKeys, result.ErrorMessage ?? "Stamp failed.", ct).ConfigureAwait(false);
                    totalFailed += result.FailedKeys.Count;
                }

                batchesCompleted++;
                EmitProgressIfDue("Assets");

                if (ReachedMaxBatches())
                    break;
            }

            EmitProgress("Assets", force: true);

            return new StampRunSummary
            {
                Phase = "Assets",
                BatchesCompleted = batchesCompleted,
                TotalClaimed = totalClaimed,
                TotalDone = totalDone,
                TotalFailed = totalFailed,
                Elapsed = sw.Elapsed
            };

            bool ReachedMaxBatches() => options.MaxBatches.HasValue && batchesCompleted >= options.MaxBatches.Value;

            void EmitProgressIfDue(string phase)
            {
                if (options.Progress == null) return;
                if (sw.Elapsed < nextProgressAt) return;
                EmitProgress(phase, force: false);
                nextProgressAt = sw.Elapsed + options.ProgressInterval;
            }

            void EmitProgress(string phase, bool force)
            {
                if (options.Progress == null && !force) return;

                var elapsed = sw.Elapsed;
                var itemsPerSec = elapsed.TotalSeconds > 0.5 ? (totalDone + totalFailed) / elapsed.TotalSeconds : 0.0;

                options.Progress?.Invoke(new ProgressInfo
                {
                    Phase = phase,
                    BatchesCompleted = batchesCompleted,
                    TotalClaimed = totalClaimed,
                    TotalDone = totalDone,
                    TotalFailed = totalFailed,
                    Elapsed = elapsed,
                    ItemsPerSecond = itemsPerSec,
                    EstimatedTimeRemaining = null
                });
            }
        }

        public static Task<StampRunSummary> StampAllAprimoAssetsPerItemAsync(
            RestampPipelineRepository repo,
            Func<AssetStampRow, CancellationToken, Task> stampOneAsync,
            StamperOptions options,
            CancellationToken ct = default)
        {
            if (stampOneAsync == null) throw new ArgumentNullException(nameof(stampOneAsync));

            return StampAllAprimoAssetsPerItemAsync(
                repo,
                async (row, token) =>
                {
                    await stampOneAsync(row, token).ConfigureAwait(false);
                    return ItemStampResult.Ok();
                },
                options,
                ct);
        }

        public static Task<StampRunSummary> StampAllAprimoAssetsPerItemAsync(
            RestampPipelineRepository repo,
            Func<AssetStampRow, CancellationToken, Task<ItemStampResult>> stampOneAsync,
            StamperOptions options,
            CancellationToken ct = default)
        {
            if (stampOneAsync == null) throw new ArgumentNullException(nameof(stampOneAsync));

            return StampAllAprimoAssets(
                repo,
                (rows, token) => StampBatchPerItemAsync(rows, stampOneAsync, options, token),
                options,
                ct);
        }

        /// <summary>
        /// ✅ UPDATED: bounded worker model. No "rows.Count tasks" explosion.
        /// </summary>
        private static async Task<BatchStampResult> StampBatchPerItemAsync(
            List<AssetStampRow> rows,
            Func<AssetStampRow, CancellationToken, Task<ItemStampResult>> stampOneAsync,
            StamperOptions options,
            CancellationToken ct)
        {
            var done = new ConcurrentBag<string>();
            var failedErrors = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var queue = new ConcurrentQueue<AssetStampRow>(rows);

            var workers = new List<Task>(options.MaxDegreeOfParallelism);
            for (int i = 0; i < options.MaxDegreeOfParallelism; i++)
            {
                workers.Add(Task.Run(async () =>
                {
                    while (!ct.IsCancellationRequested && queue.TryDequeue(out var row))
                    {
                        try
                        {
                            var result = await ExecuteWithRetryResultAsync(
                                dictKey: row.DictKey,
                                action: () => stampOneAsync(row, ct),
                                options: options,
                                ct: ct).ConfigureAwait(false);

                            if (result != null && result.Success)
                                done.Add(row.DictKey);
                            else
                                failedErrors[row.DictKey] = result?.ErrorMessage ?? "Stamp failed.";
                        }
                        catch (Exception ex)
                        {
                            failedErrors[row.DictKey] = ex.Message;
                        }
                    }
                }, ct));
            }

            await Task.WhenAll(workers).ConfigureAwait(false);

            return new BatchStampResult
            {
                DoneKeys = done.ToList(),
                FailedKeyErrors = failedErrors.Count > 0 ? new Dictionary<string, string>(failedErrors) : null,
                ErrorMessage = failedErrors.Count > 0 ? failedErrors.Values.FirstOrDefault() : null
            };
        }

        #endregion

        #region ImageSets (optimized + backward-compatible)

        public static Task StampAllImageSets(
            RestampPipelineRepository repo,
            Func<List<ImageSetStampRow>, CancellationToken, Task> stampBatchAsync,
            int batchSize = 2000,
            CancellationToken ct = default)
        {
            var opts = new StamperOptions { BatchSize = batchSize };
            return StampAllImageSets(repo,
                async (rows, token) =>
                {
                    await stampBatchAsync(rows, token).ConfigureAwait(false);
                    return new BatchStampResult
                    {
                        DoneKeys = rows.Select(r => r.DictKey).ToList(),
                        FailedKeys = new List<string>()
                    };
                },
                opts,
                ct);
        }

        public static Task StampAllImageSets(
            RestampPipelineRepository repo,
            Func<List<ImageSetStampRow>, CancellationToken, Task<BatchStampResult>> stampBatchAsync,
            int batchSize = 2000,
            int? maxBatches = null,
            CancellationToken ct = default)
        {
            var opts = new StamperOptions { BatchSize = batchSize, MaxBatches = maxBatches };
            return StampAllImageSets(repo, stampBatchAsync, opts, ct);
        }

        public static async Task<StampRunSummary> StampAllImageSets(
            RestampPipelineRepository repo,
            Func<List<ImageSetStampRow>, CancellationToken, Task<BatchStampResult>> stampBatchAsync,
            StamperOptions options,
            CancellationToken ct = default)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            if (stampBatchAsync == null) throw new ArgumentNullException(nameof(stampBatchAsync));
            options ??= new StamperOptions();
            if (options.BatchSize <= 0) throw new ArgumentOutOfRangeException(nameof(options.BatchSize));
            if (options.MaxDegreeOfParallelism <= 0) throw new ArgumentOutOfRangeException(nameof(options.MaxDegreeOfParallelism));

            var sw = Stopwatch.StartNew();
            var nextProgressAt = TimeSpan.Zero;

            long totalClaimed = 0;
            long totalDone = 0;
            long totalFailed = 0;
            int batchesCompleted = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var keys = await repo.ClaimNextImageSetBatchAsync(options.BatchSize, ct).ConfigureAwait(false);
                if (keys == null || keys.Count == 0)
                    break;

                totalClaimed += keys.Count;

                List<ImageSetStampRow> rows;
                try
                {
                    rows = await repo.FetchImageSetJsonForKeysAsync(keys, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await repo.MarkImageSetsFailedAsync(keys, ex.Message, ct).ConfigureAwait(false);
                    totalFailed += keys.Count;
                    batchesCompleted++;
                    EmitProgressIfDue("ImageSets");
                    if (ReachedMaxBatches()) break;
                    continue;
                }

                var rowKeys = new HashSet<string>(rows.Select(r => r.DictKey), StringComparer.OrdinalIgnoreCase);
                var missing = keys.Where(k => !rowKeys.Contains(k)).ToList();
                if (missing.Count > 0)
                {
                    await repo.MarkImageSetsFailedAsync(missing, "Missing ImageSets and/or ImageSetsRelations row(s) for DictKey.", ct).ConfigureAwait(false);
                    totalFailed += missing.Count;
                }

                if (rows.Count == 0)
                {
                    batchesCompleted++;
                    EmitProgressIfDue("ImageSets");
                    if (ReachedMaxBatches()) break;
                    continue;
                }

                BatchStampResult result;
                try
                {
                    result = await stampBatchAsync(rows, ct).ConfigureAwait(false) ?? new BatchStampResult();
                }
                catch (Exception ex)
                {
                    await repo.MarkImageSetsFailedAsync(rows.Select(r => r.DictKey), ex.Message, ct).ConfigureAwait(false);
                    totalFailed += rows.Count;
                    batchesCompleted++;
                    EmitProgressIfDue("ImageSets");
                    if (ReachedMaxBatches()) break;
                    continue;
                }

                if (result.DoneKeys != null && result.DoneKeys.Count > 0)
                {
                    await repo.MarkImageSetsDoneAsync(result.DoneKeys, ct).ConfigureAwait(false);
                    totalDone += result.DoneKeys.Count;
                }

                if (result.FailedKeyErrors != null && result.FailedKeyErrors.Count > 0)
                {
                    await MarkFailedGroupedAsync(
                        markFailed: (list, msg) => repo.MarkImageSetsFailedAsync(list, msg, ct),
                        failedKeyErrors: result.FailedKeyErrors).ConfigureAwait(false);

                    totalFailed += result.FailedKeyErrors.Count;
                }
                else if (result.FailedKeys != null && result.FailedKeys.Count > 0)
                {
                    await repo.MarkImageSetsFailedAsync(result.FailedKeys, result.ErrorMessage ?? "Stamp failed.", ct).ConfigureAwait(false);
                    totalFailed += result.FailedKeys.Count;
                }

                batchesCompleted++;
                EmitProgressIfDue("ImageSets");

                if (ReachedMaxBatches())
                    break;
            }

            EmitProgress("ImageSets", force: true);

            return new StampRunSummary
            {
                Phase = "ImageSets",
                BatchesCompleted = batchesCompleted,
                TotalClaimed = totalClaimed,
                TotalDone = totalDone,
                TotalFailed = totalFailed,
                Elapsed = sw.Elapsed
            };

            bool ReachedMaxBatches() => options.MaxBatches.HasValue && batchesCompleted >= options.MaxBatches.Value;

            void EmitProgressIfDue(string phase)
            {
                if (options.Progress == null) return;
                if (sw.Elapsed < nextProgressAt) return;
                EmitProgress(phase, force: false);
                nextProgressAt = sw.Elapsed + options.ProgressInterval;
            }

            void EmitProgress(string phase, bool force)
            {
                if (options.Progress == null && !force) return;

                var elapsed = sw.Elapsed;
                var itemsPerSec = elapsed.TotalSeconds > 0.5 ? (totalDone + totalFailed) / elapsed.TotalSeconds : 0.0;

                options.Progress?.Invoke(new ProgressInfo
                {
                    Phase = phase,
                    BatchesCompleted = batchesCompleted,
                    TotalClaimed = totalClaimed,
                    TotalDone = totalDone,
                    TotalFailed = totalFailed,
                    Elapsed = elapsed,
                    ItemsPerSecond = itemsPerSec,
                    EstimatedTimeRemaining = null
                });
            }
        }

        public static Task<StampRunSummary> StampAllImageSetsPerItemAsync(
            RestampPipelineRepository repo,
            Func<ImageSetStampRow, CancellationToken, Task> stampOneAsync,
            StamperOptions options,
            CancellationToken ct = default)
        {
            if (stampOneAsync == null) throw new ArgumentNullException(nameof(stampOneAsync));

            return StampAllImageSetsPerItemAsync(
                repo,
                async (row, token) =>
                {
                    await stampOneAsync(row, token).ConfigureAwait(false);
                    return ItemStampResult.Ok();
                },
                options,
                ct);
        }

        public static Task<StampRunSummary> StampAllImageSetsPerItemAsync(
            RestampPipelineRepository repo,
            Func<ImageSetStampRow, CancellationToken, Task<ItemStampResult>> stampOneAsync,
            StamperOptions options,
            CancellationToken ct = default)
        {
            if (stampOneAsync == null) throw new ArgumentNullException(nameof(stampOneAsync));

            return StampAllImageSets(
                repo,
                (rows, token) => StampImageSetBatchPerItemAsync(rows, stampOneAsync, options, token),
                options,
                ct);
        }

        /// <summary>
        /// ✅ UPDATED: bounded worker model. No "rows.Count tasks" explosion.
        /// </summary>
        private static async Task<BatchStampResult> StampImageSetBatchPerItemAsync(
            List<ImageSetStampRow> rows,
            Func<ImageSetStampRow, CancellationToken, Task<ItemStampResult>> stampOneAsync,
            StamperOptions options,
            CancellationToken ct)
        {
            var done = new ConcurrentBag<string>();
            var failedErrors = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var queue = new ConcurrentQueue<ImageSetStampRow>(rows);

            var workers = new List<Task>(options.MaxDegreeOfParallelism);
            for (int i = 0; i < options.MaxDegreeOfParallelism; i++)
            {
                workers.Add(Task.Run(async () =>
                {
                    while (!ct.IsCancellationRequested && queue.TryDequeue(out var row))
                    {
                        try
                        {
                            var result = await ExecuteWithRetryResultAsync(
                                dictKey: row.DictKey,
                                action: () => stampOneAsync(row, ct),
                                options: options,
                                ct: ct).ConfigureAwait(false);

                            if (result != null && result.Success)
                                done.Add(row.DictKey);
                            else
                                failedErrors[row.DictKey] = result?.ErrorMessage ?? "Stamp failed.";
                        }
                        catch (Exception ex)
                        {
                            failedErrors[row.DictKey] = ex.Message;
                        }
                    }
                }, ct));
            }

            await Task.WhenAll(workers).ConfigureAwait(false);

            return new BatchStampResult
            {
                DoneKeys = done.ToList(),
                FailedKeyErrors = failedErrors.Count > 0 ? new Dictionary<string, string>(failedErrors) : null,
                ErrorMessage = failedErrors.Count > 0 ? failedErrors.Values.FirstOrDefault() : null
            };
        }

        #endregion

        #region Retry helpers

        private static async Task<ItemStampResult> ExecuteWithRetryResultAsync(
            string dictKey,
            Func<Task<ItemStampResult>> action,
            StamperOptions options,
            CancellationToken ct)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            options ??= new StamperOptions();

            var shouldRetry = options.ShouldRetry ?? DefaultShouldRetry;
            var attempt = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                attempt++;

                try
                {
                    var result = await action().ConfigureAwait(false);

                    if (result == null)
                        return ItemStampResult.Fail("Stamp failed (null result).");

                    if (result.Success)
                        return result;

                    if (result.Retryable && attempt <= options.MaxRetries)
                        throw new StampRetryableFailureException(result.ErrorMessage ?? "Retryable failure.");

                    return result;
                }
                catch (Exception ex) when (attempt <= options.MaxRetries && shouldRetry(ex))
                {
                    var delay = ComputeBackoffDelay(options, attempt);
                    options.OnRetry?.Invoke(new RetryInfo { DictKey = dictKey, Attempt = attempt, Delay = delay, Exception = ex });
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }
        }

        private sealed class StampRetryableFailureException : Exception
        {
            public StampRetryableFailureException(string message) : base(message) { }
        }

        private static TimeSpan ComputeBackoffDelay(StamperOptions options, int attempt)
        {
            var baseMs = options.InitialBackoff.TotalMilliseconds;
            var exp = Math.Min(6, attempt);
            var candidate = TimeSpan.FromMilliseconds(baseMs * Math.Pow(2, exp - 1));

            if (candidate > options.MaxBackoff)
                candidate = options.MaxBackoff;

            var jitterFactor = 0.8 + (Random.Shared.NextDouble() * 0.4);
            var jittered = TimeSpan.FromMilliseconds(candidate.TotalMilliseconds * jitterFactor);

            if (jittered < TimeSpan.FromMilliseconds(50))
                jittered = TimeSpan.FromMilliseconds(50);

            return jittered;
        }

        private static bool DefaultShouldRetry(Exception ex)
        {
            if (ex is TimeoutException) return true;

            var t = ex.GetType();
            if (t.Name == "HttpRequestException")
            {
                var statusProp = t.GetProperty("StatusCode");
                if (statusProp != null)
                {
                    var statusVal = statusProp.GetValue(ex);
                    if (statusVal != null)
                    {
                        var codeInt = (int)statusVal;
                        if (codeInt == 408 || codeInt == 429) return true;
                        if (codeInt >= 500 && codeInt <= 599) return true;
                    }
                }

                return true;
            }

            var msg = ex.Message ?? string.Empty;
            if (msg.Contains("429") || msg.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
                return true;

            if (t.Name.Contains("SqlException", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static async Task MarkFailedGroupedAsync(
            Func<List<string>, string, Task> markFailed,
            Dictionary<string, string> failedKeyErrors)
        {
            foreach (var grp in failedKeyErrors
                         .GroupBy(kvp => string.IsNullOrWhiteSpace(kvp.Value) ? "Stamp failed." : kvp.Value))
            {
                var keys = grp.Select(x => x.Key).ToList();
                var msg = grp.Key;
                await markFailed(keys, msg).ConfigureAwait(false);
            }
        }

        #endregion
    }
}
