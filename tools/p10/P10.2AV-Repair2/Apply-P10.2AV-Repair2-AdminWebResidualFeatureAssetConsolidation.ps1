Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = $PSScriptRoot
    while ($null -ne $current -and $current -ne '') {
        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web', 'src')
        if ([System.IO.Directory]::Exists($candidate)) {
            return $current
        }
        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) { break }
        $current = $parent.FullName
    }
    throw 'Unable to locate repository root from script location.'
}

function Add-Line {
    param(
        [object] $Lines,
        [string] $Text
    )
    [void]$Lines.Add($Text)
}

function Ensure-Directory {
    param([string] $Path)
    if (-not [System.IO.Directory]::Exists($Path)) {
        [void][System.IO.Directory]::CreateDirectory($Path)
    }
}

function Move-IfSafe {
    param(
        [string] $Source,
        [string] $Destination,
        [string] $Label,
        [object] $Report
    )

    $sourceExists = [System.IO.File]::Exists($Source)
    $destinationExists = [System.IO.File]::Exists($Destination)

    if ($sourceExists -and $destinationExists) {
        Add-Line -Lines $Report -Text ('- Skipped {0}: source and destination both exist; no overwrite performed.' -f $Label)
        Write-Host ('Skipped {0}; destination already exists and source remains for manual review.' -f $Label)
        return
    }

    if (-not $sourceExists -and $destinationExists) {
        Add-Line -Lines $Report -Text ('- Already consolidated {0}: destination exists.' -f $Label)
        Write-Host ('Already consolidated {0}' -f $Label)
        return
    }

    if (-not $sourceExists -and -not $destinationExists) {
        Add-Line -Lines $Report -Text ('- Not present {0}: neither source nor destination was found.' -f $Label)
        Write-Host ('Not present {0}' -f $Label)
        return
    }

    $destinationDirectory = [System.IO.Path]::GetDirectoryName($Destination)
    Ensure-Directory -Path $destinationDirectory
    Move-Item -LiteralPath $Source -Destination $Destination
    Add-Line -Lines $Report -Text ('- Moved {0}: {1}' -f $Label, $Destination)
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

$repoRoot = Get-RepositoryRoot
$sourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$flatApiRoot = [System.IO.Path]::Combine($sourceRoot, 'api')
$flatTypesRoot = [System.IO.Path]::Combine($sourceRoot, 'types')
$featureRoot = [System.IO.Path]::Combine($sourceRoot, 'features')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AV-Repair2-AdminWebResidualFeatureAssetConsolidation.md')
Ensure-Directory -Path ([System.IO.Path]::GetDirectoryName($reportPath))

$report = New-Object System.Collections.ArrayList
Add-Line -Lines $report -Text '# P10.2AV Repair2 - Admin Web Residual Feature Asset Consolidation'
Add-Line -Lines $report -Text ''
Add-Line -Lines $report -Text 'This repair intentionally avoids import rewriting. It only moves residual flat API/type files into existing canonical feature folders when that can be done without overwriting files.'
Add-Line -Lines $report -Text ''

$items = @(
    [pscustomobject]@{ Feature = 'operations/runtimeDashboard'; Api = 'runtimeDashboardApi.ts'; Types = 'runtimeDashboard.ts' },
    [pscustomobject]@{ Feature = 'operations/executionSessions'; Api = 'executionSessionsApi.ts'; Types = 'executionSessions.ts' },
    [pscustomobject]@{ Feature = 'operations/failureRetry'; Api = 'failureRetryApi.ts'; Types = 'failureRetry.ts' },
    [pscustomobject]@{ Feature = 'operations/executionWorkerTelemetry'; Api = 'executionWorkerTelemetryApi.ts'; Types = 'executionWorkerTelemetry.ts' },
    [pscustomobject]@{ Feature = 'operations/commandCenter'; Api = 'commandCenterApi.ts'; Types = 'commandCenter.ts' },
    [pscustomobject]@{ Feature = 'operations/operationalEvents'; Api = 'operationalEventsApi.ts'; Types = 'operationalEvents.ts' },
    [pscustomobject]@{ Feature = 'operations/executionProfiles'; Api = 'executionProfilesApi.ts'; Types = 'executionProfiles.ts' },
    [pscustomobject]@{ Feature = 'operations/capacityForecast'; Api = 'capacityForecastApi.ts'; Types = 'capacityForecast.ts' },
    [pscustomobject]@{ Feature = 'platform/capacityForecast'; Api = 'capacityForecastApi.ts'; Types = 'capacityForecast.ts' },
    [pscustomobject]@{ Feature = 'platform/costAnalytics'; Api = 'costAnalyticsApi.ts'; Types = 'costAnalytics.ts' },
    [pscustomobject]@{ Feature = 'security/credentialVault'; Api = 'credentialVaultApi.ts'; Types = 'credentialVault.ts' },
    [pscustomobject]@{ Feature = 'connectors/configuration'; Api = 'connectorConfigurationApi.ts'; Types = 'connectorConfiguration.ts' },
    [pscustomobject]@{ Feature = 'governance/notificationRouting'; Api = 'notificationRoutingApi.ts'; Types = 'notificationRouting.ts' },
    [pscustomobject]@{ Feature = 'governance/auditTrail'; Api = 'auditTrailApi.ts'; Types = 'auditTrail.ts' }
)

foreach ($item in $items) {
    $featureDirectory = [System.IO.Path]::Combine($featureRoot, $item.Feature)
    if (-not [System.IO.Directory]::Exists($featureDirectory)) {
        Add-Line -Lines $report -Text ('- Skipped {0}: feature folder does not exist.' -f $item.Feature)
        continue
    }

    if ($item.Api -ne '') {
        $sourceApi = [System.IO.Path]::Combine($flatApiRoot, $item.Api)
        $destinationApi = [System.IO.Path]::Combine($featureDirectory, 'api', $item.Api)
        Move-IfSafe -Source $sourceApi -Destination $destinationApi -Label ($item.Feature + ' API') -Report $report
    }

    if ($item.Types -ne '') {
        $sourceTypes = [System.IO.Path]::Combine($flatTypesRoot, $item.Types)
        $destinationTypes = [System.IO.Path]::Combine($featureDirectory, 'types', $item.Types)
        Move-IfSafe -Source $sourceTypes -Destination $destinationTypes -Label ($item.Feature + ' types') -Report $report
    }
}

Add-Line -Lines $report -Text ''
Add-Line -Lines $report -Text 'Next step: run the paired test script, rebuild Admin Web, and commit only after both succeed.'
[System.IO.File]::WriteAllLines($reportPath, [string[]]$report)
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2AV Repair2 residual feature asset consolidation applied.'
