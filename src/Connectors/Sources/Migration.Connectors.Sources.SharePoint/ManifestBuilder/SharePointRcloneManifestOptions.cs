using Migration.Connectors.Sources.SharePoint.Configuration;
using Migration.ControlPlane.ManifestBuilder;
using Newtonsoft.Json.Linq;

namespace Migration.Connectors.Sources.SharePoint.ManifestBuilder;

public sealed class SharePointRcloneManifestOptions
{
    public string RcloneExecutablePath { get; init; } = "rclone";
    public string? RcloneConfigPath { get; init; }
    public string RemoteName { get; init; } = "sharepoint";
    public string RootPath { get; init; } = string.Empty;
    public string? IncludeExtensions { get; init; }
    public string? ExcludeExtensions { get; init; }
    public bool IncludeHidden { get; init; }
    public bool IncludeFolderMetadata { get; init; } = true;
    public bool IncludeFileNameMetadata { get; init; } = true;
    public int MaxFolderDepth { get; init; }

    public static SharePointRcloneManifestOptions FromRequest(
        BuildSourceManifestRequest request,
        SharePointSourceOptions configuredOptions)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(configuredOptions);

        var values = request.Options;

        return new SharePointRcloneManifestOptions
        {
            RcloneExecutablePath =
                ManifestOptionReader.GetString(values, "rcloneExecutablePath", "rclonePath", "executablePath")
                ?? configuredOptions.Rclone.ExecutablePath
                ?? "rclone",

            RcloneConfigPath =
                ManifestOptionReader.GetString(values, "rcloneConfigPath", "configPath")
                ?? configuredOptions.Rclone.ConfigPath,

            RemoteName =
                ManifestOptionReader.GetString(values, "remoteName", "rcloneRemoteName")
                ?? configuredOptions.Rclone.RemoteName
                ?? "sharepoint",

            RootPath =
                ManifestOptionReader.GetString(values, "rootPath", "sourcePath", "sharePointRootPath", "rcloneRootPath")
                ?? configuredOptions.Rclone.RootPath
                ?? string.Empty,

            IncludeExtensions =
                ManifestOptionReader.GetString(values, "includeExtensions", "extensions"),

            ExcludeExtensions =
                ManifestOptionReader.GetString(values, "excludeExtensions"),

            IncludeHidden =
                ManifestOptionReader.GetBool(values, "includeHidden")
                ?? false,

            IncludeFolderMetadata =
                ManifestOptionReader.GetBool(values, "includeFolderMetadata")
                ?? configuredOptions.Manifest.IncludeFolderMetadata,

            IncludeFileNameMetadata =
                ManifestOptionReader.GetBool(values, "includeFileNameMetadata")
                ?? configuredOptions.Manifest.IncludeFileNameMetadata,

            MaxFolderDepth =
                ManifestOptionReader.GetInt(values, "maxFolderDepth")
                ?? 0
        };
    }
}

internal static class ManifestOptionReader
{
    public static string? GetString(
        IReadOnlyDictionary<string, string>? values,
        params string[] keys)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value))
            {
                var normalized = Normalize(value);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    return normalized.Trim();
                }
            }
        }

        return null;
    }

    public static bool? GetBool(
        IReadOnlyDictionary<string, string>? values,
        params string[] keys)
    {
        var text = GetString(values, keys);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (bool.TryParse(text, out var result))
        {
            return result;
        }

        if (int.TryParse(text, out var intResult))
        {
            return intResult != 0;
        }

        return null;
    }

    public static int? GetInt(
        IReadOnlyDictionary<string, string>? values,
        params string[] keys)
    {
        var text = GetString(values, keys);
        return int.TryParse(text, out var result) ? result : null;
    }

    private static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();

        try
        {
            var token = JToken.Parse(text);

            return token.Type switch
            {
                JTokenType.String => token.Value<string>(),
                JTokenType.Boolean => token.Value<bool>().ToString(),
                JTokenType.Integer => token.Value<int>().ToString(),
                JTokenType.Float => token.Value<decimal>().ToString(),
                JTokenType.Null => null,
                _ => token.ToString(Newtonsoft.Json.Formatting.None)
            };
        }
        catch
        {
            return text;
        }
    }
}
