using Migration.Shared.Workflows.AemToAprimo.Mapping;
using Migration.Shared.Workflows.AemToAprimo.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Migration.Connectors.Targets.Aprimo.Models
{
    public sealed class AprimoImageSet
    {


        // --- Product metadata ---
        [JsonProperty("dam:MIMEtype")]
        public string? DamMIMEtype { get; set; }

        [JsonProperty("dam:scene7File")]
        public string? DamScene7File { get; set; }

        [JsonProperty("dam:scene7Folder")]
        public string? DamScene7Folder { get; set; }

        [JsonProperty("dam:scene7ID")]
        public string? DamScene7ID { get; set; }

        [JsonProperty("dam:scene7LastModified")]
        public string? DamScene7LastModified { get; set; }

        [JsonProperty("dam:scene7Name")]
        public string? DamScene7Name { get; set; }

        [JsonProperty("dam:scene7PublishTimeStamp")]
        public string? DamScene7PublishTimeStamp { get; set; }

        [JsonProperty("dam:scene7Type")]
        public string? DamScene7Type { get; set; }

        [JsonProperty("dam:scene7UploadTimeStamp")]
        public string? DamScene7UploadTimeStamp { get; set; }

        [JsonProperty("dam:sha1")]
        public string? DamSha1 { get; set; }

        [JsonProperty("dc:format")]
        public string? DcFormat { get; set; }

        [JsonProperty("dc:title")]
        [JsonConverter(typeof(CommaJoinStringOrArrayConverter))]
        [AprimoField("DisplayTitle", "Text")]
        public string? DcTitle { get; set; }


        [JsonProperty("products:ExclCommentAvail")]
        [AprimoField("itemExclCommentAvailable", "Classification List")]
        public string? ProductsExclCommentAvail { get; set; }

        [JsonProperty("products:folderCategory")]
        public string? ProductsFolderCategory { get; set; }

        [JsonProperty("products:folderSubCategory")]
        public string? ProductsFolderSubCategory { get; set; }

        [JsonProperty("products:homestoreonly")]
        public string? ProductsHomeStoreOnly { get; set; }

        [JsonProperty("products:introductiondate")]
        [AprimoField("itemMarketIntroDate", "Text")]
        public string? ProductsIntroductionDate { get; set; }

        [JsonProperty("products:itemcode")]
        [AprimoField("itemCode", "Text List")]
        public string? ProductsItemCode { get; set; }

        [JsonProperty("products:ItemDead")]
        [AprimoField("IsItemDead", "Classification List")]
        public string? ProductsItemDead { get; set; }

        [JsonProperty("products:ItemExclusiveComment")]
        [AprimoField("itemExclusiveComment", "Text List")]
        public string? ProductsItemExclusiveComment { get; set; }

        [JsonProperty("products:itemnumber")]
        [AprimoField("itemNumber", "Text List")]
        public string? ProductsItemNumber { get; set; }

        [JsonProperty("products:itemretailbrandname")]
        [AprimoField("itemRetailBrandName", "Text List")]
        public string? ProductsItemRetailBrandName { get; set; }


        [JsonProperty("products:itemsecstatus1")]
        public string? ProductsItemSecStatus1 { get; set; }

        [JsonProperty("products:itemsecstatus2")]
        public string? ProductsItemSecStatus2 { get; set; }

        [JsonProperty("products:itemsecstatus3")]
        public string? ProductsItemSecStatus3 { get; set; }

        [JsonProperty("products:itemsecstatus4")]
        public string? ProductsItemSecStatus4 { get; set; }

        [JsonProperty("products:itemsecstatus5")]
        public string? ProductsItemSecStatus5 { get; set; }

        [JsonProperty("products:itemsecstatus6")]
        public string? ProductsItemSecStatus6 { get; set; }

        [JsonProperty("products:itemsecstatus7")]
        public string? ProductsItemSecStatus7 { get; set; }

        [JsonProperty("products:itemstatus")]
        [AprimoField("itemStatus", "Classification List")]
        public string? ProductsItemStatus { get; set; }

        [JsonProperty("products:ItemStatusChanged")]
        [AprimoField("itemStatusChangedDate", "Date")]
        public string? ProductsItemStatusChanged { get; set; }

        [JsonProperty("products:ItemThirdPartyItem")]
        [AprimoField("isItemThirdPartyItem", "Classification List")]
        public string? ProductsItemThirdPartyItem { get; set; }

        [JsonProperty("products:itemVendorId")]
        public string? ProductsItemVendorId { get; set; }

        //[JsonProperty("products:itemVendorName")]  // setting this by rules
        [AprimoField("itemVendorName", "Classification List")]
        public string? ProductsItemVendorName { get; set; }

        [JsonProperty("products:keywords")]
        [JsonConverter(typeof(CommaJoinStringOrArrayConverter))]
        public string? ProductsPimKeywords { get; set; }

        [JsonProperty("products:moodshoot")]
        public string? ProductsMoodShoot { get; set; }

        [JsonProperty("products:photographytype")]
        public string? ProductsPhotographyType { get; set; }

        [JsonProperty("products:productpath")]
        public string? ProductsProductPath { get; set; }

        [JsonProperty("products:rundate")]
        public string? ProductsRunDate { get; set; }

        [JsonProperty("products:secondaryitemnumber1")]
        public string? ProductsSecondaryItemNumber1 { get; set; }

        [JsonProperty("products:secondaryitemnumber2")]
        public string? ProductsSecondaryItemNumber2 { get; set; }

        [JsonProperty("products:secondaryitemnumber3")]
        public string? ProductsSecondaryItemNumber3 { get; set; }

        [JsonProperty("products:seriesname")]
        [AprimoField("itemSeriesName", "Text List")]
        public string? ProductsSeriesName { get; set; }

        [JsonProperty("products:seriesnumber")]
        [AprimoField("itemSeriesNumber", "Text List")]
        public string? ProductsSeriesNumber { get; set; }

        [JsonProperty("products:showdiscontinued")]
        [AprimoField("showDiscontinued", "Option List")]
        public string? ProductsShowDiscontinued { get; set; }

        [JsonProperty("products:sourceid")]
        [AprimoField("sourceId", "Text")]
        public string? ProductsSourceId { get; set; }

        [JsonProperty("products:StatusChangedDate")]
        public string? ProductsStatusChangedDate { get; set; }

        [JsonProperty("products:synonyms")]
        public string? ProductsSynonyms { get; set; }

        [JsonProperty("products:wrongitem")]
        [AprimoField("isValidItemNumber", "Option List")]
        public string? ProductsWrongItem { get; set; }

        [JsonProperty("ListCultureFields:ItemGeneralColor")]
        [AprimoField("itemGeneralColor", "Text List")]
        public string? ListCultureFieldsItemGeneralColor { get; set; }

        [JsonProperty("Series:Fields:SeriesStyle")]
        [AprimoField("itemSeriesStyle", "Classification List")]
        public string? SeriesFieldsSeriesStyle { get; set; }

        [JsonProperty("ListFields:ItemMarketingTheme")]
        [AprimoField("itemMarketingTheme", "Classification List")]
        public string? ListFieldsItemMarketingTheme { get; set; }

        [JsonProperty("ListCultureFields:ItemLifestyle")]
        [AprimoField("itemInteriorDesignLifestyle", "Classification List")]
        public string? ListCultureFieldsItemLifestyle { get; set; }

        [JsonProperty("products:packageWholesaleId")]
        [JsonConverter(typeof(CommaJoinStringOrArrayConverter))] //was not expecting this
        [AprimoField("packageWholesaleId", "Text List")]
        public string? ProductsPackageWholesaleId { get; set; }

        [JsonProperty("products:packageId")]
        [AprimoField("packageId", "Text List")]
        public string? ProductsPackageId { get; set; }

        [JsonProperty("products:packageIsThirdParty")]
        [AprimoField("packageIsThirdParty", "Classification List")]
        public string? ProductsPackageIsThirdParty { get; set; }

        [JsonProperty("products:packageItemQuantity")]
        [AprimoField("packageItemQuantity", "Text List")]
        public string? ProductsPackageItemQuantity { get; set; }

        [JsonProperty("products:packageStatus")]
        [AprimoField("packageStatus", "Classification List")]
        public string? ProductsPackageStatus { get; set; }

        [JsonProperty("products:packageHero")]
        [AprimoField("packageHeroSKU", "Text List")]
        public string? ProductsPackageHero { get; set; }

        [JsonProperty("products:homeStorePackageId")]
        [AprimoField("packageHomestorePackageId", "Text List")]
        public string? ProductsHomeStorePackageId { get; set; }

        [JsonProperty("products:packageSeriesName")]
        [AprimoField("packageSeriesName", "Text List")]
        public string? ProductsPackageSeriesName { get; set; }

        [JsonProperty("products:DMSVendor")]
        [AprimoField("DMSVendor", "Text")]
        public string? ProductsDMSVendor { get; set; }

        [JsonProperty("products:DMSMRPNumber")]
        [AprimoField("DMSMRPNumber", "Text")]
        public string? ProductsDMSMRPNumber { get; set; }

        [JsonProperty("products:DMSMaterialName")]
        [AprimoField("DMSMaterialName", "Text List")]
        public string? ProductsDMSMaterialName { get; set; }

        [JsonProperty("products:DMSPartNumber")]
        [AprimoField("DMSPartNumber", "Text List")]
        public string? ProductsDMSPartNumber { get; set; }

        [JsonProperty("products:DMSColor")]
        [AprimoField("DMSColor", "Text List")]
        public string? ProductsDMSColor { get; set; }

        [JsonProperty("products:DMSSeriesusedon")]
        [AprimoField("DMSSeriesUsedOn", "Text List")]
        public string? ProductsDMSSeriesUsedOn { get; set; }

        // ============================
        // Fields NOT present in Excel rows (previously missing from AssetMetadata)
        // These must be read from your metadata.json sidecar (if present)
        // ============================

        [JsonProperty("dc:rights")]
        [JsonConverter(typeof(CommaJoinStringOrArrayConverter))]
        [AprimoField("CopyrightInformation", "Text", MetadataValueSource.JsonSidecar)]
        public string? DcRights { get; set; }

        [JsonProperty("dc:creator")]
        [JsonConverter(typeof(CommaJoinStringOrArrayConverter))]
        [AprimoField("Creator", "Text", MetadataValueSource.JsonSidecar)]
        public string? DcCreator { get; set; }

        [JsonProperty("dc:description")]
        [AprimoField("Description", "Text", MetadataValueSource.JsonSidecar)]
        public string? DcDescription { get; set; }

        [JsonProperty("xmpRights:Owner")]
        [JsonConverter(typeof(CommaJoinStringOrArrayConverter))]
        [AprimoField("CopyrightOwner", "Text", MetadataValueSource.JsonSidecar)]
        public string? XmpRightsOwner { get; set; }

        [JsonProperty("prism:expirationDate")]
        [AprimoField("ExpirationDate", "Date Time", MetadataValueSource.JsonSidecar)]
        public string? PrismExpirationDate { get; set; }

        [JsonProperty("photoshop:ICCProfile")]
        [AprimoField("ICCProfile", "Text", MetadataValueSource.JsonSidecar)]
        public string? PhotoshopICCProfile { get; set; }

        [JsonProperty("products:itemcomponentquantity")]
        [JsonConverter(typeof(CommaJoinStringOrArrayConverter))]
        [AprimoField("itemComponentQuantity", "Text List", MetadataValueSource.JsonSidecar)]
        public string? ProductsItemComponentQuantity { get; set; }

        [JsonProperty("products:RetailBrandNameOverride")]
        [AprimoField("itemRetailBrandNameOverride", "Classification List", MetadataValueSource.JsonSidecar)]
        public string? ProductsRetailBrandNameOverride { get; set; }

        [JsonProperty("products:seasonal")]
        [JsonConverter(typeof(CommaJoinStringOrArrayConverter))]
        [AprimoField("itemSeasonal", "Classification List", MetadataValueSource.JsonSidecar)]
        public string? ProductsSeasonal { get; set; }


        [AprimoField("productsAEMCreationDate", "Date Time")]
        public string? ProductsAEMCreationDate { get; set; }




        // --- ImageSet assets (nested JSON) ---
        [AprimoField("productsRecordLink", "RecordLink")]
        public AprimoImageSetAssets? AprimoImageSetAssets { get; set; }

        // --- for tracking
        public string? PathToImageSet { get; set; }

        [AprimoField("ImageSetId", "Single Line Text")]
        public string? ImageSetId { get; set; }


        [AprimoField("lastTouchedUTC", "Date Time")]
        public string? LastTouchedUTC { get; set; }
    }
}
