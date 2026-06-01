Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = Get-Location
    while ($null -ne $current) {
        $marker = [System.IO.Path]::Combine($current.Path, 'src', 'Admin', 'Migration.Admin.Web')
        if (Test-Path -Path $marker -PathType Container) {
            return $current.Path
        }
        $parent = Split-Path -Path $current.Path -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }
        $current = Get-Item -LiteralPath $parent
    }
    throw 'Unable to locate repository root containing src/Admin/Migration.Admin.Web.'
}

function Assert-Leaf {
    param([string]$Path, [string]$Label)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Missing required file for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-Container {
    param([string]$Path, [string]$Label)
    if (-not (Test-Path -Path $Path -PathType Container)) {
        throw ('Missing required folder for {0}: {1}' -f $Label, $Path)
    }
}

$repoRoot = Get-RepositoryRoot
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featuresRoot = [System.IO.Path]::Combine($sourceRoot, 'features')
Assert-Container -Path $featuresRoot -Label 'Admin Web features root'

$expectedFeatureFolders = @(
    'operations/runtimeDashboard',
    'operations/executionSessions',
    'operations/failureRetry',
    'operations/executionWorkerTelemetry',
    'operations/commandCenter',
    'operations/operationalEvents',
    'operations/executionProfiles',
    'platform/capacityForecast',
    'platform/costAnalytics',
    'security/credentialVault',
    'connectors/configuration',
    'governance/notificationRouting',
    'governance/auditTrail'
)

foreach ($feature in $expectedFeatureFolders) {
    $featurePath = [System.IO.Path]::Combine($featuresRoot, ($feature -replace '/', [System.IO.Path]::DirectorySeparatorChar))
    Assert-Container -Path $featurePath -Label ('feature {0}' -f $feature)
}

$docsDir = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
Assert-Leaf -Path ([System.IO.Path]::Combine($docsDir, 'P10.2AV-Repair3-AdminWebResidualFeatureAssetConsolidation.md')) -Label 'P10.2AV Repair3 report'

$toolDir = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AV-Repair3')
Assert-Leaf -Path ([System.IO.Path]::Combine($toolDir, 'Apply-P10.2AV-Repair3-AdminWebResidualFeatureAssetConsolidation.ps1')) -Label 'P10.2AV Repair3 apply script'
Assert-Leaf -Path ([System.IO.Path]::Combine($toolDir, 'Test-P10.2AV-Repair3-AdminWebResidualFeatureAssetConsolidation.ps1')) -Label 'P10.2AV Repair3 test script'

Write-Host 'P10.2AV Repair3 validation passed.'
