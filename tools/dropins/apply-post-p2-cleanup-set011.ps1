$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadRoot = Join-Path $repoRoot "tools\dropins\post-p2-cleanup-set011-endpoint-organization"

Write-Host "Applying Post-P2 Cleanup Set 011 from $repoRoot"

$docSource = Join-Path $payloadRoot "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_011_ENDPOINT_ORGANIZATION.md"
$docTarget = Join-Path $repoRoot "docs\post-p2-cleanup\POST_P2_CLEANUP_SET_011_ENDPOINT_ORGANIZATION.md"

if (!(Test-Path (Split-Path $docTarget -Parent))) {
    New-Item -ItemType Directory -Path (Split-Path $docTarget -Parent) -Force | Out-Null
}

Copy-Item $docSource $docTarget -Force
Write-Host "Verified docs\post-p2-cleanup\POST_P2_CLEANUP_SET_011_ENDPOINT_ORGANIZATION.md"

function Move-EndpointFile {
    param(
        [string]$FileName,
        [string]$Folder
    )

    $source = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\$FileName"
    $destinationFolder = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\$Folder"
    $destination = Join-Path $destinationFolder $FileName

    if (!(Test-Path $source)) {
        return
    }

    if (!(Test-Path $destinationFolder)) {
        New-Item -ItemType Directory -Path $destinationFolder -Force | Out-Null
    }

    if (Test-Path $destination) {
        Write-Host "Already moved $FileName -> Endpoints\$Folder"
        return
    }

    git mv $source $destination
    Write-Host "Moved $FileName -> Endpoints\$Folder"
}

$moveMap = @{
    # Cloud/provider/storage diagnostics
    "CloudPlatformEndpointExtensions.cs" = "Cloud"
    "CloudCredentialDiagnosticsEndpointExtensions.cs" = "Cloud"
    "CloudCredentialValueProbeEndpointExtensions.cs" = "Cloud"
    "CloudStoragePlanEndpointExtensions.cs" = "Cloud"
    "CloudBinaryStorageProbeEndpointExtensions.cs" = "Cloud"
    "AzureBlobStorageDiagnosticsEndpointExtensions.cs" = "Cloud"
    "CloudReadinessEndpointExtensions.cs" = "Cloud"
    "CloudConfigurationAuditEndpointExtensions.cs" = "Cloud"
    "DeploymentProfileEndpointExtensions.cs" = "Cloud"

    # Queue
    "QueueProviderPlanEndpointExtensions.cs" = "Queue"
    "QueueContractEndpointExtensions.cs" = "Queue"
    "QueueIdempotencyEndpointExtensions.cs" = "Queue"
    "QueueDispatchEndpointExtensions.cs" = "Queue"
    "AzureQueueDispatchEndpointExtensions.cs" = "Queue"
    "QueueReceiveEndpointExtensions.cs" = "Queue"
    "QueueWorkerLoopDiagnosticsEndpointExtensions.cs" = "Queue"
    "QueuePoisonHandlingEndpointExtensions.cs" = "Queue"
    "QueueFailureArtifactEndpointExtensions.cs" = "Queue"
    "QueueFailureHandlerEndpointExtensions.cs" = "Queue"
    "QueueExecutionPlannerEndpointExtensions.cs" = "Queue"
    "QueueExecutorCoordinatorEndpointExtensions.cs" = "Queue"
    "QueueExecutionObservabilityEndpointExtensions.cs" = "Queue"
    "QueueExecutionReadinessEndpointExtensions.cs" = "Queue"

    # Audit
    "AuditPersistenceEndpointExtensions.cs" = "Audit"
    "AuditArtifactPersistenceEndpointExtensions.cs" = "Audit"
    "AuditEventWriterEndpointExtensions.cs" = "Audit"
    "QueueAuditEventEndpointExtensions.cs" = "Audit"
    "CloudOperationAuditEndpointExtensions.cs" = "Audit"
    "AuditEventContractEndpointExtensions.cs" = "Audit"

    # Telemetry
    "TelemetrySinkEndpointExtensions.cs" = "Telemetry"
    "TelemetryEventWriterEndpointExtensions.cs" = "Telemetry"
    "QueueTelemetryEventEndpointExtensions.cs" = "Telemetry"
    "CloudOperationTelemetryEndpointExtensions.cs" = "Telemetry"
    "TelemetryCorrelationEndpointExtensions.cs" = "Telemetry"

    # Auth / policy
    "AuthorizationPolicyPlanEndpointExtensions.cs" = "Auth"
    "AuthenticationConfigurationEndpointExtensions.cs" = "Auth"
    "AuthPolicyReadinessEndpointExtensions.cs" = "Auth"
    "EndpointPolicyInventoryEndpointExtensions.cs" = "Auth"
    "CredentialAccessPolicyReadinessEndpointExtensions.cs" = "Auth"
    "AuthEnforcementDiagnosticsEndpointExtensions.cs" = "Auth"

    # Operations/readiness/governance
    "OperationalReadinessEndpointExtensions.cs" = "Operations"
    "ProductionSafetyGateEndpointExtensions.cs" = "Operations"
    "OperationalModeEndpointExtensions.cs" = "Operations"
    "QueueExecutionGovernanceEndpointExtensions.cs" = "Operations"
    "P2ReadinessReportEndpointExtensions.cs" = "Operations"

    # Artifacts
    "ArtifactStorageProbeEndpointExtensions.cs" = "Artifacts"
    "ArtifactManifestIndexEndpointExtensions.cs" = "Artifacts"
    "ArtifactStorageBridgeEndpointExtensions.cs" = "Artifacts"
    "ArtifactStoragePlanEndpointExtensions.cs" = "Artifacts"
    "ArtifactEndpointExtensions.cs" = "Artifacts"

    # Projects
    "ProjectEndpointExtensions.cs" = "Projects"
    "ProjectArtifactBindingEndpointExtensions.cs" = "Projects"
    "ProjectCredentialBindingEndpointExtensions.cs" = "Projects"

    # Runs
    "RunEndpointExtensions.cs" = "Runs"
    "RunExecutionPolicyEndpointExtensions.cs" = "Runs"
    "RunMonitoringEndpointExtensions.cs" = "Runs"
    "PreflightEndpointExtensions.cs" = "Runs"

    # Workspace
    "WorkspaceContextEndpointExtensions.cs" = "Workspace"
    "WorkspaceStoragePlanEndpointExtensions.cs" = "Workspace"

    # Connectors
    "ConnectorCatalogEndpointExtensions.cs" = "Connectors"
    "ConnectorCapabilityEndpointExtensions.cs" = "Connectors"

    # System/control
    "AdminSystemEndpointExtensions.cs" = "System"
    "OperationalHealthEndpointExtensions.cs" = "System"
    "ControlPlaneDeleteEndpointExtensions.cs" = "System"

    # Legacy/builder endpoint groups
    "MappingBuilderEndpointExtensions.cs" = "Builders"
    "ManifestBuilderEndpointExtensions.cs" = "Builders"
    "TaxonomyBuilderEndpointExtensions.cs" = "Builders"
}

foreach ($entry in $moveMap.GetEnumerator()) {
    Move-EndpointFile -FileName $entry.Key -Folder $entry.Value
}

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"

if (!(Test-Path $programPath)) {
    throw "Program.cs was not found at $programPath"
}

$program = [System.IO.File]::ReadAllText($programPath)

if ([string]::IsNullOrWhiteSpace($program)) {
    throw "Program.cs is empty or unreadable."
}

$helperPattern = '(?s)\r?\nstatic void AddMigrationAdminApiCloudServices\(\s*Microsoft\.Extensions\.DependencyInjection\.IServiceCollection services,\s*Microsoft\.Extensions\.Configuration\.IConfiguration configuration\)\s*\{\s*(?<service>.*?)\s*\}\s*\r?\nstatic void MapMigrationAdminApiCloudEndpoints\(\s*Microsoft\.AspNetCore\.Routing\.RouteGroupBuilder api\)\s*\{\s*(?<endpoint>.*?)\s*\}\s*$'

$match = [regex]::Match($program, $helperPattern)

if ($match.Success) {
    $serviceBody = $match.Groups["service"].Value.Trim()
    $endpointBody = $match.Groups["endpoint"].Value.Trim()

    $registrationFolder = Join-Path $repoRoot "src\Migration.Admin.Api\Registration"
    if (!(Test-Path $registrationFolder)) {
        New-Item -ItemType Directory -Path $registrationFolder -Force | Out-Null
    }

    $registrationPath = Join-Path $registrationFolder "AdminApiCloudStartupExtensions.cs"

    $registrationContent = @"
namespace Migration.Admin.Api.Registration;

public static class AdminApiCloudStartupExtensions
{
    public static void AddMigrationAdminApiCloudServices(
        Microsoft.Extensions.DependencyInjection.IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
$($serviceBody -split "`r?`n" | ForEach-Object { "        $_" } | Out-String)
    }

    public static void MapMigrationAdminApiCloudEndpoints(
        Microsoft.AspNetCore.Routing.RouteGroupBuilder api)
    {
$($endpointBody -split "`r?`n" | ForEach-Object { "        $_" } | Out-String)
    }
}
"@

    [System.IO.File]::WriteAllText($registrationPath, $registrationContent, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Created src\Migration.Admin.Api\Registration\AdminApiCloudStartupExtensions.cs"

    $program = [regex]::Replace($program, $helperPattern, "", 1)

    $program = $program.Replace(
        "AddMigrationAdminApiCloudServices(builder.Services, builder.Configuration);",
        "AdminApiCloudStartupExtensions.AddMigrationAdminApiCloudServices(builder.Services, builder.Configuration);")

    $program = $program.Replace(
        "MapMigrationAdminApiCloudEndpoints(api);",
        "AdminApiCloudStartupExtensions.MapMigrationAdminApiCloudEndpoints(api);")

    [System.IO.File]::WriteAllText($programPath, $program.TrimEnd() + "`r`n", [System.Text.UTF8Encoding]::new($false))
    Write-Host "Moved Program.cs local helper functions to AdminApiCloudStartupExtensions."
}
else {
    Write-Host "No Program.cs local cloud helper functions found. Program.cs left unchanged."
}

Write-Host ""
Write-Host "Post-P2 Cleanup Set 011 applied."
Write-Host "Run:"
Write-Host "  dotnet build .\src\Migration.Admin.Api\Migration.Admin.Api.csproj"
Write-Host "Then start Admin API and run:"
Write-Host "  powershell -ExecutionPolicy Bypass -File .\tools\test\validate-p2-completion.ps1 -BaseUrl http://localhost:5173 -AllowUnconfigured"
