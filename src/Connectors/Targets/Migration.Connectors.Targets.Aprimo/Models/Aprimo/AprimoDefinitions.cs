using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Migration.Connectors.Targets.Aprimo.Models.Aprimo
{
    public sealed class AprimoDefinitions
    {
        [JsonProperty("_links")]
        public AprimoPagedLinks? Links { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("items")]
        public List<AprimoFieldDefinition> Items { get; set; } = new List<AprimoFieldDefinition>();
    }

    public sealed class AprimoFieldDefinition
    {
        [JsonProperty("_links")]
        public AprimoFieldDefinitionLinks? Links { get; set; }

        // Common fields seen in your sample
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("label")]
        public string? Label { get; set; }

        [JsonProperty("labels")]
        public List<AprimoLocalizedLabel>? Labels { get; set; }

        [JsonProperty("dataType")]
        public string? DataType { get; set; }

        [JsonProperty("scope")]
        public string? Scope { get; set; }

        [JsonProperty("scopeCategory")]
        public string? ScopeCategory { get; set; }

        [JsonProperty("indexed")]
        public bool? Indexed { get; set; }

        [JsonProperty("languageMode")]
        public string? LanguageMode { get; set; }

        [JsonProperty("memberships")]
        public List<string>? Memberships { get; set; }

        [JsonProperty("searchIndexRebuildRequired")]
        public bool? SearchIndexRebuildRequired { get; set; }

        [JsonProperty("enabledLanguages")]
        public List<string>? EnabledLanguages { get; set; }

        [JsonProperty("isRequired")]
        public bool? IsRequired { get; set; }

        [JsonProperty("isReadOnly")]
        public bool? IsReadOnly { get; set; }

        [JsonProperty("isUniqueIdentifier")]
        public bool? IsUniqueIdentifier { get; set; }

        [JsonProperty("inlineStyle")]
        public string? InlineStyle { get; set; }

        [JsonProperty("sortIndex")]
        public int? SortIndex { get; set; }

        [JsonProperty("tag")]
        public string? Tag { get; set; }

        [JsonProperty("createdOn")]
        public DateTimeOffset? CreatedOn { get; set; }

        [JsonProperty("modifiedOn")]
        public DateTimeOffset? ModifiedOn { get; set; }

        // Some definitions include these (present on text fields / numeric fields)
        [JsonProperty("minimumLength")]
        public int? MinimumLength { get; set; }

        [JsonProperty("maximumLength")]
        public int? MaximumLength { get; set; }

        [JsonProperty("regularExpression")]
        public string? RegularExpression { get; set; }

        [JsonProperty("accuracy")]
        public double? Accuracy { get; set; }

        [JsonProperty("range")]
        public string? Range { get; set; }

        [JsonProperty("aiEnabled")]
        public bool? AiEnabled { get; set; }

        [JsonProperty("metadataPredictionEnabled")]
        public bool? MetadataPredictionEnabled { get; set; }

        [JsonProperty("hints")]
        public object? Hints { get; set; }

        // Defaults / validation
        [JsonProperty("defaultValue")]
        public string? DefaultValue { get; set; }

        [JsonProperty("resetToDefaultTriggers")]
        public List<string>? ResetToDefaultTriggers { get; set; }

        [JsonProperty("resetToDefaultFields")]
        public List<string>? ResetToDefaultFields { get; set; }

        [JsonProperty("validation")]
        public string? Validation { get; set; }

        [JsonProperty("validationErrorMessage")]
        public string? ValidationErrorMessage { get; set; }

        [JsonProperty("validationTrigger")]
        public string? ValidationTrigger { get; set; }

        [JsonProperty("storageMode")]
        public string? StorageMode { get; set; }

        // Help text
        [JsonProperty("helpText")]
        public string? HelpText { get; set; }

        [JsonProperty("helpTexts")]
        public List<AprimoLocalizedLabel>? HelpTexts { get; set; }

        // OptionList fields include "items" and a few extra props
        [JsonProperty("items")]
        public List<AprimoOptionItem>? OptionItems { get; set; }

        [JsonProperty("acceptMultipleOptions")]
        public bool? AcceptMultipleOptions { get; set; }

        [JsonProperty("sortOrder")]
        public string? SortOrder { get; set; }

        [JsonProperty("filter")]
        public string? Filter { get; set; }

        // ClassificationList fields often have a root classification id (name varies across tenants/versions)
        // Keep these as optional placeholders so your deserializer won’t choke when you encounter them.
        [JsonProperty("rootId")]
        public string? RootId { get; set; }

        [JsonProperty("rootClassificationId")]
        public string? RootClassificationId { get; set; }
    }

    public sealed class AprimoOptionItem
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("label")]
        public string? Label { get; set; }

        [JsonProperty("labels")]
        public List<AprimoLocalizedLabel>? Labels { get; set; }

        [JsonProperty("sortIndex")]
        public int? SortIndex { get; set; }

        [JsonProperty("image")]
        public object? Image { get; set; }

        [JsonProperty("disabledInDAMUI")]
        public bool? DisabledInDAMUI { get; set; }
    }

    public sealed class AprimoPagedLinks
    {
        [JsonProperty("self")]
        public AprimoLink? Self { get; set; }

        [JsonProperty("next")]
        public AprimoLink? Next { get; set; }

        [JsonProperty("last")]
        public AprimoLink? Last { get; set; }
    }

    public sealed class AprimoFieldDefinitionLinks
    {
        [JsonProperty("createdby")]
        public AprimoLink? CreatedBy { get; set; }

        [JsonProperty("modifiedby")]
        public AprimoLink? ModifiedBy { get; set; }

        [JsonProperty("self")]
        public AprimoLink? Self { get; set; }
    }

    public sealed class AprimoLink
    {
        [JsonProperty("href")]
        public string? Href { get; set; }

        [JsonProperty("select-key")]
        public string? SelectKey { get; set; }
    }
}
