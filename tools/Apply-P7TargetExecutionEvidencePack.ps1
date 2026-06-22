Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot
$packRoot = Join-Path $repoRoot '.p7-target-execution-evidence'

function Copy-PackFile {
    param(
        [Parameter(Mandatory=$true)][string]$RelativePath
    )

    $source = Join-Path $packRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath
    if (!(Test-Path -LiteralPath $source)) { throw ('Pack file missing: ' + $source) }

    $targetDirectory = Split-Path -Parent $target
    if (!(Test-Path -LiteralPath $targetDirectory)) { New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null }
    if (Test-Path -LiteralPath $target) {
        $backup = $target + '.p7-target-execution-evidence.bak'
        if (!(Test-Path -LiteralPath $backup)) { Copy-Item -LiteralPath $target -Destination $backup -Force }
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host ('Applied ' + $RelativePath)
}

function Add-TextAfter {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Needle,
        [Parameter(Mandatory=$true)][string]$InsertText,
        [Parameter(Mandatory=$true)][string]$AlreadyText
    )

    if (!(Test-Path -LiteralPath $Path)) { throw ('Missing expected file: ' + $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($AlreadyText)) { return }
    if (!$content.Contains($Needle)) { throw ('Could not find patch anchor in ' + $Path + ': ' + $Needle) }
    $backup = $Path + '.p7-target-execution-evidence.bak'
    if (!(Test-Path -LiteralPath $backup)) { Copy-Item -LiteralPath $Path -Destination $backup -Force }
    $content = $content.Replace($Needle, $Needle + $InsertText)
    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Host ('Patched ' + $Path)
}

Copy-PackFile 'src/Core/Migration.Admin.Api/Contracts/TargetExecutionEvidenceContracts.cs'
Copy-PackFile 'src/Core/Migration.Admin.Api/Endpoints/Runs/TargetExecutionEvidenceEndpointExtensions.cs'
Copy-PackFile 'src/Admin/Migration.Admin.Web/src/features/operations/targetEvidence/types/targetEvidence.ts'
Copy-PackFile 'src/Admin/Migration.Admin.Web/src/features/operations/targetEvidence/api/targetEvidenceApi.ts'
Copy-PackFile 'src/Admin/Migration.Admin.Web/src/features/operations/targetEvidence/pages/TargetExecutionEvidence.tsx'
Copy-PackFile 'src/Admin/Migration.Admin.Web/src/components/Layout.tsx'
Copy-PackFile 'src/Admin/Migration.Admin.Web/src/styles/p7-target-execution-evidence.css'

$startup = Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Registration/AdminApiEndpointStartupExtensions.cs'
Add-TextAfter -Path $startup -Needle '        api.MapRunEndpoints();' -InsertText "`r`n        api.MapTargetExecutionEvidenceEndpoints();" -AlreadyText 'MapTargetExecutionEvidenceEndpoints'


$app = Join-Path $repoRoot 'src/Admin/Migration.Admin.Web/src/App.tsx'
if (!(Test-Path -LiteralPath $app)) { throw ('Missing expected file: ' + $app) }
$appContent = Get-Content -LiteralPath $app -Raw
$appBackup = $app + '.p7-target-execution-evidence.bak'
if (!(Test-Path -LiteralPath $appBackup)) { Copy-Item -LiteralPath $app -Destination $appBackup -Force }
if (!$appContent.Contains('features/operations/targetEvidence/pages/TargetExecutionEvidence')) {
    $runImport = "import { Runs } from './features/operations/runs/pages/Runs';"
    if (!$appContent.Contains($runImport)) { throw ('Could not find Runs import in ' + $app) }
    $targetImport = "import { TargetExecutionEvidence } from './features/operations/targetEvidence/pages/TargetExecutionEvidence';"
    $appContent = $appContent.Replace($runImport, $runImport + "`r`n" + $targetImport)
}
if (!$appContent.Contains('path="runs/:runId/target-evidence"')) {
    $runRoute = '        <Route path="runs/:runId" element={<RunDetail />} />'
    if (!$appContent.Contains($runRoute)) { throw ('Could not find RunDetail route in ' + $app) }
    $targetRoutes = '        <Route path="runs/:runId/target-evidence" element={<TargetExecutionEvidence />} />' + "`r`n" + '        <Route path="target-evidence" element={<TargetExecutionEvidence />} />'
    $appContent = $appContent.Replace($runRoute, $runRoute + "`r`n" + $targetRoutes)
}
Set-Content -LiteralPath $app -Value $appContent -Encoding UTF8
Write-Host ('Patched ' + $app)

$runDetail = Join-Path $repoRoot 'src/Admin/Migration.Admin.Web/src/features/operations/runs/pages/RunDetail.tsx'
if (!(Test-Path -LiteralPath $runDetail)) { throw ('Missing expected file: ' + $runDetail) }
$runDetailContent = Get-Content -LiteralPath $runDetail -Raw
if (!$runDetailContent.Contains('Target Evidence')) {
    $needle = '{run && <button type="button" onClick={exportTimeline}>Export Timeline</button>}'
    if (!$runDetailContent.Contains($needle)) { throw ('Could not find Export Timeline action in ' + $runDetail) }
    $link = '            {run && <Link className="secondaryButton" to={`/runs/${encodeURIComponent(run.runId)}/target-evidence`}>Target Evidence</Link>}'
    $backup = $runDetail + '.p7-target-execution-evidence.bak'
    if (!(Test-Path -LiteralPath $backup)) { Copy-Item -LiteralPath $runDetail -Destination $backup -Force }
    $runDetailContent = $runDetailContent.Replace($needle, $needle + "`r`n" + $link)
    Set-Content -LiteralPath $runDetail -Value $runDetailContent -Encoding UTF8
    Write-Host ('Patched ' + $runDetail)
}

$styles = Join-Path $repoRoot 'src/Admin/Migration.Admin.Web/src/styles.css'
$stylesContent = Get-Content -LiteralPath $styles -Raw
if (!$stylesContent.Contains('p7-target-execution-evidence.css')) {
    Add-Content -LiteralPath $styles -Value "`r`n@import './styles/p7-target-execution-evidence.css';"
    Write-Host ('Patched ' + $styles)
}

Write-Host 'P7 target execution evidence pack applied.'
