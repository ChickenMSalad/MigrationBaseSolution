using System.Security.Cryptography;
using System.Text;

namespace Migration.Admin.Api.OperationalStore;

internal static class OperationalMirrorIdFactory
{
    public static Guid CreateGuid(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Value must be provided to create a deterministic operational mirror id.",
                nameof(value));
        }

        var bytes = MD5.HashData(
            Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant()));

        return new Guid(bytes);
    }
}
