using System.Globalization;
using System.Text;
using Migration.Connectors.Sources.Aem.Models;

namespace Migration.Connectors.Sources.Aem.ManifestBuilder;

internal static class AemManifestCsvWriter
{
    public static byte[] WriteManifestCsv(IEnumerable<AemAsset> assets)
    {
        var sb = new StringBuilder();

        WriteRow(sb,
        [
            nameof(AemAsset.Id),
            nameof(AemAsset.Name),
            nameof(AemAsset.Path),
            nameof(AemAsset.MimeType),
            nameof(AemAsset.SizeBytes),
            nameof(AemAsset.Created),
            nameof(AemAsset.LastModified)
        ]);

        foreach (var asset in assets)
        {
            WriteRow(sb,
            [
                asset.Id,
                asset.Name,
                asset.Path,
                asset.MimeType,
                asset.SizeBytes.ToString(),
                asset.Created ?? string.Empty,
                asset.LastModified ?? string.Empty
            ]);
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void WriteRow(StringBuilder sb, IReadOnlyList<string?> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(Escape(values[i] ?? string.Empty));
        }

        sb.AppendLine();
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\r') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
