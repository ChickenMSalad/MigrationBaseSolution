using System.Text.Json;

namespace Migration.Infrastructure.Taxonomy;

internal static class JsonTaxonomyHelpers
{
    public static string StringProp(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetPropertyInsensitive(element, name, out var prop))
            {
                return prop.ValueKind switch
                {
                    JsonValueKind.String => prop.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => prop.GetRawText()
                };
            }
        }

        return string.Empty;
    }

    public static bool BoolProp(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetPropertyInsensitive(element, name, out var prop)) continue;

            if (prop.ValueKind == JsonValueKind.True) return true;
            if (prop.ValueKind == JsonValueKind.False) return false;
            if (prop.ValueKind == JsonValueKind.String && bool.TryParse(prop.GetString(), out var parsed)) return parsed;
        }

        return false;
    }

    public static int? IntProp(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetPropertyInsensitive(element, name, out var prop)) continue;
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var parsed)) return parsed;
            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out parsed)) return parsed;
        }

        return null;
    }

    public static bool TryGetArray(JsonElement element, out JsonElement array, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetPropertyInsensitive(element, name, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                array = prop;
                return true;
            }
        }

        array = default;
        return false;
    }

    public static bool TryGetPropertyInsensitive(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
