using Microsoft.Extensions.Options;

namespace Migration.Infrastructure.State.OperationalStore.Sql;

public sealed class SqlOperationalStoreOptionsValidator : IValidateOptions<SqlOperationalStoreOptions>
{
    public ValidateOptionsResult Validate(string? name, SqlOperationalStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.ConnectionStringName) && string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add($"Either {nameof(SqlOperationalStoreOptions.ConnectionStringName)} or {nameof(SqlOperationalStoreOptions.ConnectionString)} is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SchemaName))
        {
            failures.Add($"{nameof(SqlOperationalStoreOptions.SchemaName)} is required.");
        }
        else if (!IsValidSchemaName(options.SchemaName))
        {
            failures.Add($"{nameof(SqlOperationalStoreOptions.SchemaName)} must contain only letters, numbers, or underscores and must start with a letter or underscore.");
        }

        if (options.CommandTimeoutSeconds is < 1 or > 600)
        {
            failures.Add($"{nameof(SqlOperationalStoreOptions.CommandTimeoutSeconds)} must be between 1 and 600 seconds.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsValidSchemaName(string schemaName)
    {
        if (schemaName.Length == 0)
        {
            return false;
        }

        char first = schemaName[0];
        if (!char.IsLetter(first) && first != '_')
        {
            return false;
        }

        for (int index = 1; index < schemaName.Length; index++)
        {
            char current = schemaName[index];
            if (!char.IsLetterOrDigit(current) && current != '_')
            {
                return false;
            }
        }

        return true;
    }
}
