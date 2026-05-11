using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoFileVersion
    {
        [JsonProperty("_links")]
        public AprimoLinks? Links { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("versionLabel")]
        public string? VersionLabel { get; set; }

        [JsonProperty("versionNumber")]
        public int? VersionNumber { get; set; }

        [JsonProperty("fileName")]
        public string? FileName { get; set; }

        [JsonProperty("fileCreatedOn")]
        public DateTimeOffset? FileCreatedOn { get; set; }

        [JsonProperty("fileModifiedOn")]
        public DateTimeOffset? FileModifiedOn { get; set; }

        [JsonProperty("metadata")]
        public object? Metadata { get; set; }

        [JsonProperty("fileSize")]
        public long? FileSize { get; set; }

        [JsonProperty("fileExtension")]
        public string? FileExtension { get; set; }

        [JsonProperty("comment")]
        public string? Comment { get; set; }

        [JsonProperty("crc32")]
        public long? Crc32 { get; set; }

        [JsonProperty("sha256")]
        public string? Sha256 { get; set; }

        [JsonProperty("content")]
        public object? Content { get; set; }

        [JsonProperty("tag")]
        public string? Tag { get; set; }

        [JsonProperty("preventDownload")]
        public bool PreventDownload { get; set; }

        [JsonProperty("isLatest")]
        public bool IsLatest { get; set; }

        [JsonProperty("aiInfluenced")]
        public string? AiInfluenced { get; set; }

        [JsonProperty("duplicateInfo")]
        public object? DuplicateInfo { get; set; }

        [JsonProperty("publicationItems")]
        public object? PublicationItems { get; set; }

        [JsonProperty("publications")]
        public object? Publications { get; set; }

        [JsonProperty("annotationInfo")]
        public object? AnnotationInfo { get; set; }

        [JsonProperty("watermarkId")]
        public string? WatermarkId { get; set; }

        [JsonProperty("watermarkType")]
        public string? WatermarkType { get; set; }

        [JsonProperty("fileState")]
        public string? FileState { get; set; }

        [JsonProperty("hasSmartActionFindings")]
        public object? HasSmartActionFindings { get; set; }

        [JsonProperty("createdOn")]
        public DateTimeOffset? CreatedOn { get; set; }

        [JsonProperty("_embedded")]
        public AprimoFileVersionEmbedded? Embedded { get; set; }
    }


    public sealed class AprimoFileVersionEmbedded
    {
        [JsonProperty("filetype")]
        public AprimoFileType? FileType { get; set; }
    }

    public sealed class AprimoFileType
    {
        [JsonProperty("_links")]
        public AprimoLinks? Links { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("kind")]
        public string? Kind { get; set; }

        [JsonProperty("extension")]
        public string? Extension { get; set; }

        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }

        [JsonProperty("labels")]
        public List<AprimoLabel>? Labels { get; set; }

        [JsonProperty("registeredFields")]
        public List<object>? RegisteredFields { get; set; }

        [JsonProperty("registeredFieldGroups")]
        public List<object>? RegisteredFieldGroups { get; set; }

        [JsonProperty("engineFormat")]
        public string? EngineFormat { get; set; }

        [JsonProperty("isCatalogable")]
        public bool? IsCatalogable { get; set; }

        [JsonProperty("catalogActions")]
        public List<AprimoCatalogAction>? CatalogActions { get; set; }

        [JsonProperty("mediaEngines")]
        public List<string>? MediaEngines { get; set; }

        [JsonProperty("previewFormat")]
        public string? PreviewFormat { get; set; }

        [JsonProperty("previewPlayers")]
        public List<string>? PreviewPlayers { get; set; }

        [JsonProperty("previewRequired")]
        public bool? PreviewRequired { get; set; }

        [JsonProperty("previewKeepDimensions")]
        public bool? PreviewKeepDimensions { get; set; }

        [JsonProperty("supportAssetResize")]
        public bool? SupportAssetResize { get; set; }

        [JsonProperty("supportAssetWatermark")]
        public bool? SupportAssetWatermark { get; set; }

        [JsonProperty("preferredExtension")]
        public bool? PreferredExtension { get; set; }

        [JsonProperty("tag")]
        public object? Tag { get; set; }

        [JsonProperty("modifiedOn")]
        public DateTimeOffset? ModifiedOn { get; set; }

        [JsonProperty("createdOn")]
        public DateTimeOffset? CreatedOn { get; set; }
    }

    public sealed class AprimoLabel
    {
        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("languageId")]
        public string? LanguageId { get; set; }
    }

    public sealed class AprimoCatalogAction
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("isCritical")]
        public bool? IsCritical { get; set; }
    }
}
