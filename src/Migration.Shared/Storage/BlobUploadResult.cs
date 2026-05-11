using System;

namespace Migration.Shared.Storage
{
    public sealed class BlobUploadResult
    {
        public string FileName { get; init; } = string.Empty;

        public bool Success { get; init; }

        public Exception? Exception { get; init; }
    }
}
