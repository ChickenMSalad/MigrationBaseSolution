using System.Collections;
using System.Reflection;
using CloudinaryDotNet;

namespace Migration.Connectors.Targets.Cloudinary;

internal static class CloudinarySdkCompat
{
    public static void TrySetUploadProperty(object target, string propertyName, object? value)
    {
        if (target is null || value is null)
        {
            return;
        }

        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite)
        {
            return;
        }

        var converted = ConvertValue(value, property.PropertyType);
        if (converted is null && property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null)
        {
            return;
        }

        property.SetValue(target, converted);
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(targetType);
        if (nullableUnderlying is not null)
        {
            targetType = nullableUnderlying;
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType == typeof(string))
        {
            return value.ToString();
        }

        if (targetType == typeof(bool) && value is string boolString && bool.TryParse(boolString, out var parsedBool))
        {
            return parsedBool;
        }

        if (targetType == typeof(StringDictionary))
        {
            return value switch
            {
                IDictionary<string, string> stringDictionary => ToStringDictionary(stringDictionary),
                IDictionary<string, object> objectDictionary => ToStringDictionary(objectDictionary),
                _ => null
            };
        }

        if (typeof(IDictionary).IsAssignableFrom(targetType))
        {
            return value;
        }

        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }

    public static StringDictionary ToStringDictionary(IDictionary<string, string> values)
    {
        var dictionary = new StringDictionary();
        foreach (var pair in values)
        {
            dictionary[pair.Key] = pair.Value;
        }

        return dictionary;
    }


    public static StringDictionary ToStringDictionary(IDictionary<string, object?> values)
    {
        var dictionary = new StringDictionary();
        foreach (var pair in values)
        {
            if (pair.Value is null)
            {
                continue;
            }

            dictionary[pair.Key] = pair.Value switch
            {
                string s => s,
                IEnumerable<string> list => $"[{string.Join(",", list)}]",
                _ => pair.Value.ToString()
            };
        }

        return dictionary;
    }
}
