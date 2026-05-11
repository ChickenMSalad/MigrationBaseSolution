using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.S3.Configuration
{
    public sealed class S3Options
    {
        public string AccessKeyId { get; set; } = "";
        public string SecretAccessKey { get; set; } = "";
        public string Region { get; set; } = "us-east-1";
        public string BucketName { get; set; } = "";

        // Optional: use for S3-compatible endpoints (MinIO, LocalStack, etc.)
        public string? ServiceUrl { get; set; }
    }
}
