using System.Collections.Generic;
using System.Linq;

namespace MigrationBase.Core.Cloud.Azure.Capacity;

public sealed class AzureRuntimeCapacityValidationResult
{
    public static AzureRuntimeCapacityValidationResult Passed { get; } = new(true, System.Array.Empty<string>());

    public AzureRuntimeCapacityValidationResult(bool isValid, IReadOnlyCollection<string> messages)
    {
        IsValid = isValid;
        Messages = messages;
    }

    public bool IsValid { get; }

    public IReadOnlyCollection<string> Messages { get; }

    public static AzureRuntimeCapacityValidationResult FromMessages(IEnumerable<string> messages)
    {
        var materialized = messages.Where(message => !string.IsNullOrWhiteSpace(message)).ToArray();
        return materialized.Length == 0
            ? Passed
            : new AzureRuntimeCapacityValidationResult(false, materialized);
    }
}
