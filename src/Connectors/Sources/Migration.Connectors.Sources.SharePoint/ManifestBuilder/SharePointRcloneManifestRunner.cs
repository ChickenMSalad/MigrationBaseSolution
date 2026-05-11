using System.Diagnostics;
using System.Text;

namespace Migration.Connectors.Sources.SharePoint.ManifestBuilder;

public sealed class SharePointRcloneManifestRunner
{
    public async Task<IReadOnlyList<SharePointRcloneManifestItem>> ListFilesAsync(
        SharePointRcloneManifestOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var remotePath = BuildRemotePath(options.RemoteName, options.RootPath);
        var arguments = new StringBuilder();
        arguments.Append("lsf ");
        arguments.Append(Quote(remotePath));
        arguments.Append(" --recursive --files-only");

        if (!string.IsNullOrWhiteSpace(options.RcloneConfigPath))
        {
            arguments.Append(" --config ");
            arguments.Append(Quote(options.RcloneConfigPath));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = string.IsNullOrWhiteSpace(options.RcloneExecutablePath)
                ? "rclone"
                : options.RcloneExecutablePath,
            Arguments = arguments.ToString(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start rclone process.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"rclone lsf failed with exit code {process.ExitCode}.{Environment.NewLine}{stderr}");
        }

        return stdout
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(CreateItem)
            .ToArray();
    }

    private static SharePointRcloneManifestItem CreateItem(string path)
    {
        var fileName = Path.GetFileName(path);
        var folderPath = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var folderSegments = segments.Length > 1
            ? segments.Take(segments.Length - 1).ToArray()
            : Array.Empty<string>();

        return new SharePointRcloneManifestItem(
            RelativePath: path,
            FileName: fileName,
            FileNameWithoutExtension: Path.GetFileNameWithoutExtension(fileName),
            FileExtension: Path.GetExtension(fileName),
            FolderPath: folderPath,
            FolderDepth: folderSegments.Length,
            FolderSegments: folderSegments);
    }

    private static string BuildRemotePath(string remoteName, string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            throw new InvalidOperationException("A SharePoint rclone remote name is required.");
        }

        var normalizedRoot = NormalizePath(rootPath);
        return string.IsNullOrWhiteSpace(normalizedRoot)
            ? $"{remoteName.Trim()}:"
            : $"{remoteName.Trim()}:{normalizedRoot}";
    }

    private static string NormalizePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var path = value.Replace('\\', '/').Trim();
        while (path.StartsWith("/", StringComparison.Ordinal))
        {
            path = path[1..];
        }

        return path;
    }

    private static string Quote(string value)
        => $"\"{value.Replace("\"", "\\\"")}\"";
}
