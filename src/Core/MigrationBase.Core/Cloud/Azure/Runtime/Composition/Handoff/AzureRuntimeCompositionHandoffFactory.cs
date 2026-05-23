namespace MigrationBase.Core.Cloud.Azure.Runtime.Composition.Handoff;

/// <summary>
/// Creates the standard P6.1 handoff manifest without binding any host at startup.
/// </summary>
public sealed class AzureRuntimeCompositionHandoffFactory
{
    public AzureRuntimeCompositionHandoffManifest CreateDefault()
    {
        var manifest = new AzureRuntimeCompositionHandoffManifest
        {
            Summary = "Runtime composition contracts, planning, validation reporting, and host binding contracts are ready for concrete implementation wiring."
        };

        manifest.Items.Add(new AzureRuntimeCompositionHandoffItem
        {
            Area = "Composition",
            Capability = "Runtime composition bootstrap",
            OwningHostRole = "AllHosts",
            RequiresProgramComposition = false,
            Notes = "Available as a contract surface only; host startup wiring remains explicit."
        });

        manifest.Items.Add(new AzureRuntimeCompositionHandoffItem
        {
            Area = "Composition",
            Capability = "Plan building and validation reporting",
            OwningHostRole = "AllHosts",
            RequiresProgramComposition = false,
            Notes = "Supports later validation gates before worker/API/operator host startup."
        });

        manifest.Items.Add(new AzureRuntimeCompositionHandoffItem
        {
            Area = "Composition",
            Capability = "Host binding contracts",
            OwningHostRole = "Api, OperatorUi, Dispatcher, QueueWorker, ManifestIngestion",
            RequiresProgramComposition = false,
            Notes = "Prepared for P6.2/P6.3 concrete DI and hosted-service integration."
        });

        manifest.NextImplementationAreas.Add("P6.2 Azure infrastructure integration");
        manifest.NextImplementationAreas.Add("P6.3 worker execution runtime");
        manifest.NextImplementationAreas.Add("P6.4 queue and dispatcher mechanics");

        return manifest;
    }
}
