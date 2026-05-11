using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Utilities
{


    public static class GlbPreviewExtractor
    {
        public sealed record ExtractedImage(
            string FileName,
            string MimeType,
            byte[] Bytes);

        /// <summary>
        /// Reads a GLB from a stream and extracts embedded images (png/jpg/etc).
        /// </summary>
        public static List<ExtractedImage> ExtractImagesFromGlbStream(Stream glbStream)
        {
            if (glbStream == null) throw new ArgumentNullException(nameof(glbStream));

            // SharpGLTF expects a readable stream. If the stream isn't seekable, copy it first.
            Stream input = glbStream;
            if (!glbStream.CanSeek)
            {
                var ms = new MemoryStream();
                glbStream.CopyTo(ms);
                ms.Position = 0;
                input = ms;
            }
            else
            {
                // ensure we're at start
                if (glbStream.Position != 0) glbStream.Position = 0;
            }

            var model = ModelRoot.ReadGLB(input); // stream-based GLB load

            var results = new List<ExtractedImage>();

            foreach (var img in model.LogicalImages)
            {
                // Image name may be null; use index as fallback
                var name = img.Name;
                if (string.IsNullOrWhiteSpace(name))
                    name = $"image_{img.LogicalIndex}";

                // SharpGLTF exposes image bytes via Content
                var bytes = img.Content.Content.ToArray();

                // Mime type might be available depending on container; fallback by sniffing common signatures
                var mime = GuessMimeType(bytes) ?? "application/octet-stream";

                var ext = MimeToExtension(mime);
                var fileName = $"{SanitizeFileName(name)}{ext}";

                results.Add(new ExtractedImage(fileName, mime, bytes));
            }

            return results;
        }

        /// <summary>
        /// Picks a "best" preview image: prefer largest by byte size, else first.
        /// </summary>
        public static ExtractedImage? PickBestPreview(List<ExtractedImage> images)
            => images
                .OrderByDescending(i => i.Bytes?.Length ?? 0)
                .FirstOrDefault();

        private static string SanitizeFileName(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Trim();
        }

        private static string? GuessMimeType(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 12) return null;

            // PNG signature: 89 50 4E 47 0D 0A 1A 0A
            if (bytes.Length >= 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
                return "image/png";

            // JPEG signature: FF D8 FF
            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return "image/jpeg";

            // WEBP: "RIFF" .... "WEBP"
            if (bytes.Length >= 12 &&
                bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
                bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
                return "image/webp";

            return null;
        }

        private static string MimeToExtension(string mime) => mime switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            _ => ".bin"
        };
    }

}
