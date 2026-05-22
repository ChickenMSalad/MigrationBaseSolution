using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Shared.Workflows.AemToAprimo.Mapping
{
    public class CommaJoinStringOrArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                // single string
                return reader.Value.ToString();
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                // read array of strings
                var items = serializer.Deserialize<List<string>>(reader);
                return string.Join(",", items);
            }

            return null; // or string.Empty;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }

}
