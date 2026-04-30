using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Migration.Shared.Workflows.AemToAprimo.Mapping
{


    public class StringOrArrayConverter : JsonConverter<List<string>>
    {
        public override List<string>? ReadJson(JsonReader reader, Type objectType, List<string>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType == JsonToken.String)
                return new List<string> { (string)reader.Value! };

            // If it's an array, just deserialize normally
            if (reader.TokenType == JsonToken.StartArray)
                return JArray.Load(reader).ToObject<List<string>>();

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} for List<string>");
        }

        public override void WriteJson(JsonWriter writer, List<string>? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            // Always write as array
            writer.WriteStartArray();
            foreach (var s in value)
                writer.WriteValue(s);
            writer.WriteEndArray();
        }
    }

}
