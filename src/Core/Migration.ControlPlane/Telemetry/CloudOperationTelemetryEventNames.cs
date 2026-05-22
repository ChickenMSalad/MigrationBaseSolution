namespace Migration.ControlPlane.Telemetry;

public static class CloudOperationTelemetryEventNames
{
    public const string ReadinessChecked = "cloud.readiness.checked";
    public const string StorageProviderChecked = "cloud.storage.provider.checked";
    public const string CredentialProviderChecked = "cloud.credential.provider.checked";
    public const string ArtifactStorageChecked = "cloud.artifact-storage.checked";
    public const string DeploymentProfileChecked = "cloud.deployment-profile.checked";
}
