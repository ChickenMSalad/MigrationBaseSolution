using System.Text;

namespace Migration.Connectors.Sources.SharePoint.ManifestBuilder;

public static class SharePointRcloneManifestCsvWriter
{
    public static string Write(
        IReadOnlyList<SharePointRcloneManifestItem> items,
        SharePointRcloneManifestOptions options)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(options);

        var maxFolderDepth = items.Count == 0
            ? 0
            : items.Max(x => x.FolderDepth);

        if (options.MaxFolderDepth > 0)
        {
            maxFolderDepth = Math.Min(maxFolderDepth, options.MaxFolderDepth);
        }

        var headers = new List<string>
        {
            "RowId",
            "SourceAssetId",
            "SourcePath",
            "sharepoint_relative_path"
        };

        if (options.IncludeFolderMetadata)
        {
            headers.Add("folder_path");
            headers.Add("folder_depth");

            for (var i = 1; i <= maxFolderDepth; i++)
            {
                headers.Add($"folder_level_{i}");
            }
        }

        if (options.IncludeFileNameMetadata)
        {
            headers.Add("file_name");
            headers.Add("file_name_without_extension");
            headers.Add("file_extension");
        }

        headers.Add("_manifest_source");
        headers.Add("rclone_remote_name");
        headers.Add("rclone_root_path");

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(Escape)));

        var rowId = 0;
        foreach (var item in items)
        {
            rowId++;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["RowId"] = rowId.ToString(),
                ["SourceAssetId"] = item.RelativePath,
                ["SourcePath"] = item.RelativePath,
                ["sharepoint_relative_path"] = item.RelativePath,
                ["folder_path"] = item.FolderPath,
                ["folder_depth"] = item.FolderDepth.ToString(),
                ["file_name"] = item.FileName,
                ["file_name_without_extension"] = item.FileNameWithoutExtension,
                ["file_extension"] = item.FileExtension,
                ["_manifest_source"] = "SharePointRclone",
                ["rclone_remote_name"] = options.RemoteName,
                ["rclone_root_path"] = options.RootPath
            };

            for (var i = 1; i <= maxFolderDepth; i++)
            {
                values[$"folder_level_{i}"] = item.FolderSegments.Count >= i
                    ? item.FolderSegments[i - 1]
                    : string.Empty;
            }

            builder.AppendLine(string.Join(",", headers.Select(header =>
                Escape(values.TryGetValue(header, out var value) ? value : string.Empty))));
        }

        return builder.ToString();
    }

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;

        if (text.Contains('"'))
        {
            text = text.Replace("\"", "\"\"");
        }

        return text.Contains(',') || text.Contains('"') || text.Contains('\r') || text.Contains('\n')
            ? $"\"{text}\""
            : text;
    }
}
