using System.Globalization;
using System.Reflection;
using System.Text;

namespace Migration.ControlPlane.ManifestBuilder;

public static class ManifestCsvWriter
{
    public static string WriteObjects<T>(IEnumerable<T> rows, IReadOnlyList<string>? preferredColumns = null)
    {
        var items = rows?.ToList() ?? new List<T>();

        var properties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.CanRead)
            .ToArray();

        if (preferredColumns is { Count: > 0 })
        {
            properties = preferredColumns
                .Select(name => properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                .Where(p => p is not null)
                .Cast<PropertyInfo>()
                .ToArray();
        }

        var builder = new StringBuilder();

        if (properties.Length == 0)
        {
            return string.Empty;
        }

        builder.AppendLine(string.Join(",", properties.Select(p => Escape(p.Name))));

        foreach (var item in items)
        {
            builder.AppendLine(string.Join(",", properties.Select(p => Escape(Format(p.GetValue(item))))));
        }

        return builder.ToString();
    }

    private static string Format(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        var mustQuote = text.Contains(',') || text.Contains('"') || text.Contains('\r') || text.Contains('\n');

        if (text.Contains('"'))
        {
            text = text.Replace("\"", "\"\"");
        }

        return mustQuote ? $"\"{text}\"" : text;
    }
}
