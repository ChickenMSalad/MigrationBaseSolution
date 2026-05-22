using Microsoft.Extensions.Options;

namespace Migration.Infrastructure.Sql.Options;

public sealed class SqlOperationalStoreOptionsValidator : IValidateOptions<SqlOperationalStoreOptions>
{
    public ValidateOptionsResult Validate(string? name, SqlOperationalStoreOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail(
                $"{SqlOperationalStoreOptions.SectionName}:ConnectionString is required.");
        }

        if (options.CommandTimeoutSeconds <= 0)
        {
            return ValidateOptionsResult.Fail(
                $"{SqlOperationalStoreOptions.SectionName}:CommandTimeoutSeconds must be greater than zero.");
        }

        if (options.WorkItemLeaseMinutes <= 0)
        {
            return ValidateOptionsResult.Fail(
                $"{SqlOperationalStoreOptions.SectionName}:WorkItemLeaseMinutes must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}
