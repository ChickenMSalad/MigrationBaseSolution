namespace Migration.Connectors.Sources.SharePoint.Services;

internal static class SharePointPathUtilities
{
    public static string NormalizeRelativePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var path = value.Replace('\\', '/').Trim();
        while (path.StartsWith('/')) path = path[1..];
        return path;
    }

    public static string CombineRemotePath(string? rootPath, string? relativePath)
    {
        var root = NormalizeRelativePath(rootPath);
        var rel = NormalizeRelativePath(relativePath);
        if (string.IsNullOrWhiteSpace(root)) return rel;
        if (string.IsNullOrWhiteSpace(rel)) return root;
        return $"{root.TrimEnd('/')}/{rel.TrimStart('/')}";
    }

    public static Dictionary<string, string> BuildPathMetadata(string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        var folder = Path.GetDirectoryName(normalized)?.Replace('\\', '/') ?? string.Empty;
        var fileName = Path.GetFileName(normalized);
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var folderSegments = segments.Length > 1 ? segments.Take(segments.Length - 1).ToArray() : Array.Empty<string>();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sharepoint_relative_path"] = normalized,
            ["folder_path"] = folder,
            ["folder_depth"] = folderSegments.Length.ToString(),
            ["file_name"] = fileName,
            ["file_name_without_extension"] = nameWithoutExtension,
            ["file_extension"] = extension
        };

        for (var i = 0; i < folderSegments.Length; i++)
            metadata[$"folder_level_{i + 1}"] = folderSegments[i];

        return metadata;
    }

    public static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(value.Select(ch => invalid.Contains(ch) ? '_' : ch));
        return string.IsNullOrWhiteSpace(sanitized) ? "asset.bin" : sanitized;
    }

    public static string GuessContentType(string? fileName) => Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".tif" or ".tiff" => "image/tiff",
        ".pdf" => "application/pdf",
        ".json" => "application/json",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        ".mp4" => "video/mp4",
        ".mov" => "video/quicktime",
        _ => "application/octet-stream"
    };
}
