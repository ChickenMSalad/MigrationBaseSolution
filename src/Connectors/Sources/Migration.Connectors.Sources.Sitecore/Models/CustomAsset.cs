using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Migration.Connectors.Sources.Sitecore.Models
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<List<Root>>(myJsonResponse);

    public class CustomAsset
    {
        [JsonPropertyName("Id")]
        public long Id { get; set; }

        [JsonPropertyName("DefinitionName")]
        public string DefinitionName { get; set; }

        [JsonPropertyName("Properties")]
        public AssetProperties Properties { get; set; }
    }

    public class AccessTier
    {
        [JsonPropertyName("Value")]
        public string Value { get; set; }
    }

    public class AlternateFile
    {
        [JsonPropertyName("locations")]
        public Locations locations { get; set; }

        [JsonPropertyName("accessTier")]
        public AccessTier accessTier { get; set; }
    }

    public class FileProperties
    {
        [JsonPropertyName("properties")]
        public SubProperties properties { get; set; }
    }

    public class Locations
    {
        [JsonPropertyName("local")]
        public List<string> local { get; set; }

        [JsonPropertyName("builtin")]
        public List<string> builtin { get; set; }
    }

    public class MainFile
    {
        [JsonPropertyName("properties")]
        public SubProperties properties { get; set; }

        [JsonPropertyName("locations")]
        public Locations locations { get; set; }

        [JsonPropertyName("accessTier")]
        public AccessTier accessTier { get; set; }
    }

    public class Metadata
    {
        [JsonPropertyName("status")]
        public string status { get; set; }

        [JsonPropertyName("properties")]
        public SubProperties properties { get; set; }

        [JsonPropertyName("locations")]
        public Locations locations { get; set; }

        [JsonPropertyName("sticky")]
        public bool? sticky { get; set; }

        [JsonPropertyName("accessTier")]
        public AccessTier accessTier { get; set; }
    }

    public class PageImage
    {
        [JsonPropertyName("status")]
        public string status { get; set; }

        [JsonPropertyName("properties")]
        public SubProperties properties { get; set; }

        [JsonPropertyName("locations")]
        public Locations locations { get; set; }
    }

    public class Preview
    {
        [JsonPropertyName("status")]
        public string status { get; set; }

        [JsonPropertyName("properties")]
        public SubProperties properties { get; set; }

        [JsonPropertyName("locations")]
        public Locations locations { get; set; }
    }

    public class AssetProperties
    {
        [JsonPropertyName("Shopify")]
        public object Shopify { get; set; }

        [JsonPropertyName("Title")]
        public string Title { get; set; }

        [JsonPropertyName("FileName")]
        public string FileName { get; set; }

        [JsonPropertyName("TemplateProperties")]
        public object TemplateProperties { get; set; }

        [JsonPropertyName("Renditions")]
        public Renditions Renditions { get; set; }

        [JsonPropertyName("Keywords")]
        public string Keywords { get; set; }

        [JsonPropertyName("ChiliType")]
        public object ChiliType { get; set; }

        [JsonPropertyName("ReasonForRejection")]
        public object ReasonForRejection { get; set; }

        [JsonPropertyName("VirusScanResult")]
        public string VirusScanResult { get; set; }

        [JsonPropertyName("AI.TaggingStatus")]
        public object AITaggingStatus { get; set; }

        [JsonPropertyName("AI.Suggestions")]
        public object AISuggestions { get; set; }

        [JsonPropertyName("VideoAI.Language")]
        public object VideoAILanguage { get; set; }

        [JsonPropertyName("AmbassadorName")]
        public object AmbassadorName { get; set; }

        [JsonPropertyName("LegacyFolderLocation")]
        public string LegacyFolderLocation { get; set; }

        [JsonPropertyName("ImageSimilarityTags")]
        public object ImageSimilarityTags { get; set; }

        [JsonPropertyName("Structure")]
        public object Structure { get; set; }

        [JsonPropertyName("VisionDescription")]
        public object VisionDescription { get; set; }

        [JsonPropertyName("FocalPoint")]
        public object FocalPoint { get; set; }

        [JsonPropertyName("Asset.HasComplexRightsProfiles")]
        public object AssetHasComplexRightsProfiles { get; set; }

        [JsonPropertyName("ApprovedBy")]
        public string ApprovedBy { get; set; }

        [JsonPropertyName("Asset.DrmComplexity")]
        public object AssetDrmComplexity { get; set; }

        [JsonPropertyName("DeletedOn")]
        public object DeletedOn { get; set; }

        [JsonPropertyName("PublishStatusDetails")]
        public object PublishStatusDetails { get; set; }

        [JsonPropertyName("Elastic")]
        public object Elastic { get; set; }

        [JsonPropertyName("renditiontracker")]
        public object renditiontracker { get; set; }

        [JsonPropertyName("ModelName")]
        public object ModelName { get; set; }

        [JsonPropertyName("MainFile")]
        public MainFile MainFile { get; set; }

        [JsonPropertyName("PublishStatus")]
        public object PublishStatus { get; set; }

        [JsonPropertyName("CheckedOutIn")]
        public object CheckedOutIn { get; set; }

        [JsonPropertyName("Digest")]
        public string Digest { get; set; }

        [JsonPropertyName("PlumRiver")]
        public object PlumRiver { get; set; }

        [JsonPropertyName("ArchivedBy")]
        public string ArchivedBy { get; set; }

        [JsonPropertyName("RenditionsPurged")]
        public bool? RenditionsPurged { get; set; }

        [JsonPropertyName("RenditionsAccessTier")]
        public string RenditionsAccessTier { get; set; }

        [JsonPropertyName("IsDraft")]
        public object IsDraft { get; set; }

        [JsonPropertyName("ReasonForStatus")]
        public object ReasonForStatus { get; set; }

        [JsonPropertyName("VisionOcrText")]
        public object VisionOcrText { get; set; }

        [JsonPropertyName("PublicCollections.PublishStatusDetails")]
        public object PublicCollectionsPublishStatusDetails { get; set; }

        [JsonPropertyName("ExtraRenditions")]
        public object ExtraRenditions { get; set; }

        [JsonPropertyName("AlternateFile")]
        public AlternateFile AlternateFile { get; set; }

        [JsonPropertyName("FileProperties")]
        public FileProperties FileProperties { get; set; }

        [JsonPropertyName("Embargo")]
        public object Embargo { get; set; }

        [JsonPropertyName("IsDiscarding")]
        public object IsDiscarding { get; set; }

        [JsonPropertyName("ExpirationDate")]
        public DateTime? ExpirationDate { get; set; }

        [JsonPropertyName("Asset.ExplicitApprovalRequired")]
        public bool? AssetExplicitApprovalRequired { get; set; }

        [JsonPropertyName("Description")]
        public object Description { get; set; }

        [JsonPropertyName("PublicCollections.PublishStatus")]
        public object PublicCollectionsPublishStatus { get; set; }

        [JsonPropertyName("SitecoreMLStatus")]
        public object SitecoreMLStatus { get; set; }

        [JsonPropertyName("FileSize")]
        public double? FileSize { get; set; }

        [JsonPropertyName("ApprovalDate")]
        public DateTime? ApprovalDate { get; set; }

        [JsonPropertyName("NuOrder")]
        public object NuOrder { get; set; }

        [JsonPropertyName("SubtitleLanguage")]
        public object SubtitleLanguage { get; set; }

        [JsonPropertyName("CollaborationName")]
        public object CollaborationName { get; set; }

        [JsonPropertyName("AverageRating")]
        public object AverageRating { get; set; }

        [JsonPropertyName("HasDuplicate")]
        public bool? HasDuplicate { get; set; }

        [JsonPropertyName("LicensorName")]
        public object LicensorName { get; set; }

        [JsonPropertyName("AssetProductSKU")]
        public object AssetProductSKU { get; set; }

        [JsonPropertyName("ArchivalDate")]
        public DateTime? ArchivalDate { get; set; }

        [JsonPropertyName("Asset.Copyright")]
        public object AssetCopyright { get; set; }

        [JsonPropertyName("IsCheckedOut")]
        public object IsCheckedOut { get; set; }

        [JsonPropertyName("Centric")]
        public object Centric { get; set; }

        [JsonPropertyName("IsCheckingIn")]
        public object IsCheckingIn { get; set; }
    }

    public class SubProperties
    {
        [JsonPropertyName("content_type")]
        public string content_type { get; set; }

        [JsonPropertyName("filesizebytes")]
        public string filesizebytes { get; set; }

        [JsonPropertyName("width")]
        public string width { get; set; }

        [JsonPropertyName("height")]
        public string height { get; set; }

        [JsonPropertyName("filesize")]
        public double? filesize { get; set; }

        [JsonPropertyName("filename")]
        public string filename { get; set; }

        [JsonPropertyName("group")]
        public string group { get; set; }

        [JsonPropertyName("extension")]
        public string extension { get; set; }

        [JsonPropertyName("resolution")]
        public string resolution { get; set; }

        [JsonPropertyName("colorspace")]
        public string colorspace { get; set; }

        [JsonPropertyName("megapixels")]
        public string megapixels { get; set; }

        [JsonPropertyName("creator")]
        public string creator { get; set; }

        [JsonPropertyName("application")]
        public string application { get; set; }
    }

    public class Renditions
    {
        [JsonPropertyName("metadata")]
        public Metadata metadata { get; set; }

        [JsonPropertyName("preview")]
        public Preview preview { get; set; }

        [JsonPropertyName("thumbnail")]
        public Thumbnail thumbnail { get; set; }

        [JsonPropertyName("thumbnail_cropped")]
        public ThumbnailCropped thumbnail_cropped { get; set; }

        [JsonPropertyName("page_image")]
        public PageImage page_image { get; set; }
    }

    public class Thumbnail
    {
        [JsonPropertyName("status")]
        public string status { get; set; }

        [JsonPropertyName("properties")]
        public SubProperties properties { get; set; }

        [JsonPropertyName("locations")]
        public Locations locations { get; set; }
    }

    public class ThumbnailCropped
    {
        [JsonPropertyName("status")]
        public string status { get; set; }

        [JsonPropertyName("properties")]
        public SubProperties properties { get; set; }

        [JsonPropertyName("locations")]
        public Locations locations { get; set; }
    }


}
