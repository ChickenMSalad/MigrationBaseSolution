using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bynder.Sdk.Model;
using System.Collections.Generic;
using Newtonsoft.Json;
using Migration.Connectors.Targets.Bynder.Clients.Converters;

namespace Migration.Connectors.Sources.Sitecore.Models
{
    public class BynderMetaProperty
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("options")]
        public List<MetapropertyOption> Options { get; set; }

        [JsonProperty("isMultiSelect", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsMultiSelect { get; set; }

        [JsonProperty("isRequired", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsRequired { get; set; }

        [JsonProperty("isFilterable", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsFilterable { get; set; }

        [JsonProperty("isMainfilter", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsMainfilter { get; set; }

        [JsonProperty("isEditable", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsEditable { get; set; }

        [JsonProperty("zindex")]
        public int ZIndex { get; set; }

        // extended properties from REST Response
        [JsonProperty("isDisplayField", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsDisplayField { get; set; }
        [JsonProperty("isMultifilter", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsMultifilter { get; set; }
        [JsonProperty("showInGridView", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool ShowInGridView { get; set; }
        [JsonProperty("showInListView", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool ShowInListView { get; set; }
        [JsonProperty("isApiField", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsApiField { get; set; }
        [JsonProperty("showInDuplicateView", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool ShowInDuplicateView { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("isSearchable", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsSearchable { get; set; }
        [JsonProperty("isDrilldown", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool IsDrilldown { get; set; }
        [JsonProperty("useDependencies", ItemConverterType = typeof(BooleanJsonConverter))]
        public bool UseDependencies { get; set; }


    }



}
