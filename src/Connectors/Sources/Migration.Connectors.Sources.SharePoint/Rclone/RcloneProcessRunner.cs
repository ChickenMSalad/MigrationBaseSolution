using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Migration.Connectors.Sources.SharePoint.Configuration;

namespace Migration.Connectors.Sources.SharePoint.Rclone;

public sealed class RcloneProcessRunner
{
    private readonly ILogger<RcloneProcessRunner> _logger;

    public RcloneProcessRunner(ILogger<RcloneProcessRunner> logger) => _logger = logger;

    public async Task<string> RunAsync(RcloneOptions options, string arguments, CancellationToken cancellationToken)
    {
        var fullArguments = BuildArguments(options, arguments);
        var psi = new ProcessStartInfo
        {
            FileName = options.ExecutablePath,
            Arguments = fullArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        _logger.LogDebug("Executing rclone {Arguments}", Redact(fullArguments));
        if (!process.Start()) throw new InvalidOperationException("Failed to start rclone process.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (options.ProcessTimeoutSeconds > 0)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(options.ProcessTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
        }
        else
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"rclone failed with exit code {process.ExitCode}. {stderr}");

        return stdout.ToString();
    }

    private static string BuildArguments(RcloneOptions options, string arguments)
    {
        if (string.IsNullOrWhiteSpace(options.ConfigPath)) return arguments;
        return $"{arguments} --config {Quote(options.ConfigPath)}";
    }

    public static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
    private static string Redact(string value) => value.Replace("--password", "--password REDACTED", StringComparison.OrdinalIgnoreCase);
}
