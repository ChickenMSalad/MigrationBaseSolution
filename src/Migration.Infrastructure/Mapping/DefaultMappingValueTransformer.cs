using System.Globalization;
using Migration.Application.Abstractions;

namespace Migration.Infrastructure.Mapping;

/// <summary>
/// Default field-level transforms for mapping profiles.
/// Transform aliases are intentionally forgiving so older mapping files and future UI-generated mappings can coexist.
/// </summary>
public sealed class DefaultMappingValueTransformer : IMappingValueTransformer
{
    public object? Transform(MappingValueTransformContext context)
    {
        var transformName = NormalizeTransformName(context.TransformName);
        if (string.IsNullOrWhiteSpace(transformName))
        {
            return context.Value;
        }

        return transformName switch
        {
            "trim" => AsString(context.Value)?.Trim(),
            "lower" or "lowercase" or "to-lower" or "tolower" => AsString(context.Value)?.Trim().ToLowerInvariant(),
            "upper" or "uppercase" or "to-upper" or "toupper" => AsString(context.Value)?.Trim().ToUpperInvariant(),

            "split:semicolon" or "splitsemicolon" or "split-semi-colon" or "split-semi" => Split(context.Value, ';'),
            "split:comma" or "splitcomma" => Split(context.Value, ','),
            "split:pipe" or "splitpipe" => Split(context.Value, '|'),
            "split:newline" or "splitnewline" or "split:line" or "splitline" => SplitLines(context.Value),

            "normalize-date" or "normalizedate" or "date:yyyy-mm-dd" => NormalizeDate(context.Value, "yyyy-MM-dd"),
            "normalize-datetime" or "normalizedatetime" or "date:o" => NormalizeDate(context.Value, "O"),

            "boolean" or "bool" or "to-bool" or "tobool" => ToBoolean(context.Value),
            "integer" or "int" or "to-int" or "toint" => ToInteger(context.Value),
            "decimal" or "number" or "to-decimal" or "todecimal" => ToDecimal(context.Value),

            "empty-to-null" or "emptytonull" => EmptyToNull(context.Value),
            "null-if-empty" or "nullifempty" => EmptyToNull(context.Value),

            _ => context.Value
        };
    }

    private static string? NormalizeTransformName(string? transformName)
    {
        return string.IsNullOrWhiteSpace(transformName)
            ? null
            : transformName.Trim().ToLowerInvariant();
    }

    private static string? AsString(object? value) => value?.ToString();

    private static string[] Split(object? value, char separator)
    {
        var text = AsString(value);
        return string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : text.Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static string[] SplitLines(object? value)
    {
        var text = AsString(value);
        return string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static object? NormalizeDate(object? value, string format)
    {
        var text = AsString(value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            return dto.ToUniversalTime().ToString(format, CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
        {
            return dt.ToString(format, CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static object? ToBoolean(object? value)
    {
        var text = AsString(value)?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.ToLowerInvariant() switch
        {
            "true" or "t" or "yes" or "y" or "1" => true,
            "false" or "f" or "no" or "n" or "0" => false,
            _ => value
        };
    }

    private static object? ToInteger(object? value)
    {
        var text = AsString(value);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : value;
    }

    private static object? ToDecimal(object? value)
    {
        var text = AsString(value);
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : value;
    }

    private static object? EmptyToNull(object? value)
    {
        return string.IsNullOrWhiteSpace(AsString(value)) ? null : value;
    }
}
