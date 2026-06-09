using Microsoft.Extensions.Options;

namespace Migration.Admin.Api.OperationalStore;

public sealed class OperationalRunMirrorOptionsValidator
    : IValidateOptions<OperationalRunMirrorOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        OperationalRunMirrorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return ValidateOptionsResult.Success;
    }
}


