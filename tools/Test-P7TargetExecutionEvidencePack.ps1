Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot

function Assert-File {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (!(Test-Path -LiteralPath $Path)) { throw ('Missing expected file: ' + $Path) }
}

function Assert-Text {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )
    Assert-File -Path $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if (!$content.Contains($Text)) { throw ('Missing expected text in ' + $Path + ': ' + $Text) }
}

Assert-Text -Path (Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Contracts/TargetExecutionEvidenceContracts.cs') -Text 'TargetExecutionEvidenceResponse'
Assert-Text -Path (Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Endpoints/Runs/TargetExecutionEvidenceEndpointExtensions.cs') -Text '/runs/{runId}/target-evidence'
Assert-Text -Path (Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Registration/AdminApiEndpointStartupExtensions.cs') -Text 'api.MapTargetExecutionEvidenceEndpoints();'
Assert-Text -Path (Join-Path $repoRoot 'src/Admin/Migration.Admin.Web/src/App.tsx') -Text 'runs/:runId/target-evidence'
Assert-Text -Path (Join-Path $repoRoot 'src/Admin/Migration.Admin.Web/src/components/Layout.tsx') -Text 'navGroups'
Assert-Text -Path (Join-Path $repoRoot 'src/Admin/Migration.Admin.Web/src/features/operations/runs/pages/RunDetail.tsx') -Text 'Target Evidence'
Assert-Text -Path (Join-Path $repoRoot 'src/Admin/Migration.Admin.Web/src/features/operations/targetEvidence/pages/TargetExecutionEvidence.tsx') -Text 'Export Retry'
Assert-Text -Path (Join-Path $repoRoot 'src/Admin/Migration.Admin.Web/src/styles/p7-target-execution-evidence.css') -Text 'tableScrollTall'
Assert-Text -Path (Join-Path $repoRoot 'src/Admin/Migration.Admin.Web/src/styles.css') -Text 'p7-target-execution-evidence.css'

Write-Host 'P7 target execution evidence validation passed.'
