using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
namespace Migration.Connectors.Sources.Sitecore.Models
{

    public sealed class NodeBynderAssetResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("dateModifiedFrom")]
        public string? DateModifiedFrom { get; set; }

        [JsonProperty("assets")]
        public List<NodeBynderAssetDto> Assets { get; set; } = new();

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("status")]
        public int? Status { get; set; }
    }

    public sealed class NodeBynderAssetDto
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("dateCreated")]
        public string? DateCreated { get; set; }

        [JsonProperty("dateModified")]
        public string? DateModified { get; set; }

        [JsonProperty("versionNumber")]
        public int? VersionNumber { get; set; }

        [JsonProperty("archive")]
        public bool? Archive { get; set; }

        [JsonProperty("isPublic")]
        public bool? IsPublic { get; set; }

        [JsonProperty("limited")]
        public bool? Limited { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("copyright")]
        public string? Copyright { get; set; }

        [JsonProperty("brandId")]
        public string? BrandId { get; set; }

        [JsonProperty("originalUrl")]
        public string? OriginalUrl { get; set; }

        [JsonProperty("thumbnailUrl")]
        public string? ThumbnailUrl { get; set; }

        [JsonProperty("fileSize")]
        public long? FileSize { get; set; }

        [JsonProperty("extension")]
        public object? Extension { get; set; }

        // Add this only if your Node script returns it
        [JsonProperty("metaProperties")]
        public List<NodeBynderMetaPropertyDto> MetaProperties { get; set; } = new();
    }
    public sealed class NodeBynderMetaPropertyDto
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }
    }
}
