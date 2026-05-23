using System.Collections.Generic;

namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition.Binding;

public interface IAzureRuntimeCompositionBindingRegistry
{
    IReadOnlyCollection<AzureRuntimeCompositionBinding> GetBindings();

    AzureRuntimeCompositionBindingValidationResult Validate();
}
