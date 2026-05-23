using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition.Binding;

public sealed class AzureRuntimeCompositionBindingValidationResult
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    public IReadOnlyCollection<string> Errors => _errors;

    public IReadOnlyCollection<string> Warnings => _warnings;

    public bool IsValid => _errors.Count == 0;

    public void AddError(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _errors.Add(message);
        }
    }

    public void AddWarning(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _warnings.Add(message);
        }
    }
}
