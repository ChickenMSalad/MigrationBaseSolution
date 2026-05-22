namespace Migration.Shared.Storage.S3;

public static class S3PathUtilities
{
    public static string NormalizeKey(string? value)
    {
        return (value ?? string.Empty)
            .Replace('\\', '/')
            .Trim('/');
    }

    public static string CombineKey(params string?[] parts)
    {
        return string.Join("/", parts
                .Select(NormalizeKey)
                .Where(x => !string.IsNullOrWhiteSpace(x)))
            .Trim('/');
    }
}
