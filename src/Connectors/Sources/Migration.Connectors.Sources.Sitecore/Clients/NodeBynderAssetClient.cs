using System.Diagnostics;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using Migration.Connectors.Sources.Sitecore.Configuration;
using Migration.Connectors.Sources.Sitecore.Models;

namespace Migration.Connectors.Sources.Sitecore.Clients
{


    public interface INodeBynderAssetClient
    {
        Task<IReadOnlyList<NodeBynderAssetDto>> GetAssetsModifiedAfterAsync(
            DateTimeOffset modifiedAfterUtc,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Dictionary<string, string>>> GetFlatRowsModifiedAfterAsync(
            DateTimeOffset modifiedAfterUtc,
            CancellationToken cancellationToken = default);
    }

    public sealed class NodeBynderAssetClient : INodeBynderAssetClient
    {
        private readonly NodeBynderScriptOptions _options;
        private readonly ILogger<NodeBynderAssetClient> _logger;

        public NodeBynderAssetClient(
            IOptions<NodeBynderScriptOptions> options,
            ILogger<NodeBynderAssetClient> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IReadOnlyList<NodeBynderAssetDto>> GetAssetsModifiedAfterAsync(
            DateTimeOffset modifiedAfterUtc,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.WorkingDirectory))
            {
                throw new InvalidOperationException("NodeBynderScriptOptions.WorkingDirectory is required.");
            }

            if (string.IsNullOrWhiteSpace(_options.ScriptPath))
            {
                throw new InvalidOperationException("NodeBynderScriptOptions.ScriptPath is required.");
            }

            var timestamp = modifiedAfterUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.NodeExecutablePath,
                Arguments = $"\"{_options.ScriptPath}\" \"{timestamp}\"",
                WorkingDirectory = _options.WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stdout.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stderr.AppendLine(e.Data);
                }
            };

            _logger.LogInformation(
                "Starting Node Bynder retrieval script. ScriptPath: {ScriptPath}, ModifiedAfterUtc: {ModifiedAfterUtc}",
                _options.ScriptPath,
                timestamp);

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start Node process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var stdoutText = stdout.ToString().Trim();
            var stderrText = stderr.ToString().Trim();

            _logger.LogInformation(
                "Node process exited with code {ExitCode}. StdOutLength: {StdOutLength}, StdErrLength: {StdErrLength}",
                process.ExitCode,
                stdoutText.Length,
                stderrText.Length);

            if (process.ExitCode == 0)
            {
                if (string.IsNullOrWhiteSpace(stdoutText))
                {
                    throw new InvalidOperationException("Node script completed successfully but returned no stdout.");
                }

                NodeBynderAssetResponse? response;
                try
                {
                    response = JsonConvert.DeserializeObject<NodeBynderAssetResponse>(stdoutText);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to deserialize Node stdout. Output: {stdoutText}",
                        ex);
                }

                if (response == null)
                {
                    throw new InvalidOperationException("Node script returned null response.");
                }

                if (!response.Success)
                {
                    throw new InvalidOperationException(
                        $"Node script reported failure. Message: {response.Message}, Status: {response.Status}");
                }

                return response.Assets;
            }

            if (!string.IsNullOrWhiteSpace(stderrText))
            {
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<NodeBynderAssetResponse>(stderrText);
                    if (errorResponse != null)
                    {
                        throw new InvalidOperationException(
                            $"Node script failed. Message: {errorResponse.Message}, Status: {errorResponse.Status}");
                    }
                }
                catch (JsonException)
                {
                    // Ignore and fall through to raw stderr
                }
            }

            throw new InvalidOperationException(
                $"Node script failed with exit code {process.ExitCode}. STDERR: {stderrText}");
        }

        public async Task<IReadOnlyList<Dictionary<string, string>>> GetFlatRowsModifiedAfterAsync(
                DateTimeOffset modifiedAfterUtc,
                CancellationToken cancellationToken = default)
        {
            string stdoutText = await ExecuteNodeScriptAsync(modifiedAfterUtc, cancellationToken);

            NodeBynderFlatRowResponse? response;
            try
            {
                response = JsonConvert.DeserializeObject<NodeBynderFlatRowResponse>(stdoutText);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize Node stdout as NodeBynderFlatRowResponse. Output: {stdoutText}",
                    ex);
            }

            if (response == null)
            {
                throw new InvalidOperationException("Node script returned null flat row response.");
            }

            if (!response.Success)
            {
                throw new InvalidOperationException(
                    $"Node script reported failure. Message: {response.Message}, Status: {response.Status}");
            }

            return response.Rows;
        }

        private async Task<string> ExecuteNodeScriptAsync(
                DateTimeOffset modifiedAfterUtc,
                CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.WorkingDirectory))
            {
                throw new InvalidOperationException("NodeBynderScriptOptions.WorkingDirectory is required.");
            }

            if (string.IsNullOrWhiteSpace(_options.ScriptPath))
            {
                throw new InvalidOperationException("NodeBynderScriptOptions.ScriptPath is required.");
            }

            var timestamp = modifiedAfterUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.NodeExecutablePath,
                Arguments = $"\"{_options.ScriptPath}\" \"{timestamp}\"",
                WorkingDirectory = _options.WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stdout.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stderr.AppendLine(e.Data);
                }
            };

            _logger.LogInformation(
                "Starting Node Bynder script. ScriptPath: {ScriptPath}, ModifiedAfterUtc: {ModifiedAfterUtc}",
                _options.ScriptPath,
                timestamp);

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start Node process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var stdoutText = stdout.ToString().Trim();
            var stderrText = stderr.ToString().Trim();

            _logger.LogInformation(
                "Node process exited with code {ExitCode}. StdOutLength: {StdOutLength}, StdErrLength: {StdErrLength}",
                process.ExitCode,
                stdoutText.Length,
                stderrText.Length);

            if (process.ExitCode == 0)
            {
                if (string.IsNullOrWhiteSpace(stdoutText))
                {
                    throw new InvalidOperationException("Node script completed successfully but returned no stdout.");
                }

                return stdoutText;
            }

            if (!string.IsNullOrWhiteSpace(stderrText))
            {
                try
                {
                    var assetError = JsonConvert.DeserializeObject<NodeBynderAssetResponse>(stderrText);
                    if (assetError != null && !string.IsNullOrWhiteSpace(assetError.Message))
                    {
                        throw new InvalidOperationException(
                            $"Node script failed. Message: {assetError.Message}, Status: {assetError.Status}");
                    }
                }
                catch (JsonException)
                {
                    // Ignore and fall through
                }

                try
                {
                    var rowError = JsonConvert.DeserializeObject<NodeBynderFlatRowResponse>(stderrText);
                    if (rowError != null && !string.IsNullOrWhiteSpace(rowError.Message))
                    {
                        throw new InvalidOperationException(
                            $"Node script failed. Message: {rowError.Message}, Status: {rowError.Status}");
                    }
                }
                catch (JsonException)
                {
                    // Ignore and fall through
                }
            }

            throw new InvalidOperationException(
                $"Node script failed with exit code {process.ExitCode}. STDERR: {stderrText}");
        }
    }
}
