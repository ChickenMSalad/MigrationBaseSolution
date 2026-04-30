using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Utilities
{
    public static class Filters
    {

        // Regex pattern to match:
        // - Control characters (ASCII 0–31): [\x00-\x1F]
        // - Backslash \, question mark ?, hash #
        // - Names ending in . or /
        private static readonly Regex InvalidBlobNameRegex = new Regex(
            @"[\x00-\x1F\\?#]|[./]$",
            RegexOptions.Compiled
        );

        public static IEnumerable<string> FilterBlobs(
        IEnumerable<string> allBlobs,
        string? extensionType = null,
        string? size = null,
        string? batchName = null)
        {
            return allBlobs
                .Where(blob =>
                    !blob.Contains("invalids.json") &&
                    !blob.Contains("invalid.json") &&
                    !blob.Contains(".json.processed"))
                .Where(blob =>
                    string.IsNullOrEmpty(extensionType) || blob.Contains($"/{extensionType}/"))
                .Where(blob =>
                    string.IsNullOrEmpty(size) || blob.Contains($"/{size}/"))
                .Where(blob =>
                    string.IsNullOrEmpty(batchName) || blob.EndsWith($"/{batchName}.json"));
            ;
        }

        public static bool HasInvalidBlobChars(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) return true;
            return InvalidBlobNameRegex.IsMatch(filename);
        }

        public static string NormalizeSlashes(string filename)
        {
            int firstSlashIndex = filename.IndexOf('/');
            if (firstSlashIndex == -1) return filename; // no slashes

            int secondSlashIndex = filename.IndexOf('/', firstSlashIndex + 1);
            if (secondSlashIndex == -1) return filename; // only one slash

            string before = filename.Substring(0, firstSlashIndex + 1); // keep up to first slash
            string after = filename.Substring(firstSlashIndex + 1).Replace("/", "_"); // replace rest
            return before + after;
        }

    }
}
