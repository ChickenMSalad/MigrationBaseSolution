Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot

function Require-File {
    param([Parameter(Mandatory=$true)][string[]]$RelativePaths)
    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $repoRoot $relativePath
        if (Test-Path -LiteralPath $path) { return $path }
    }
    throw ('Missing expected file. Checked: ' + [string]::Join(', ', $RelativePaths))
}

function Require-Text {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [StringComparison]::Ordinal) -lt 0) {
        throw ('Missing expected text in ' + $Path + ': ' + $Text)
    }
}

function Reject-Text {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [StringComparison]::Ordinal) -ge 0) {
        throw ('Found rejected text in ' + $Path + ': ' + $Text)
    }
}

$health = Require-File @('src\Core\Migration.Admin.Api\Endpoints\System\OperationalHealthEndpointExtensions.cs')
Require-Text $health '"/api/operational/health/live"'
Require-Text $health '"/api/operational/health/ready"'

$gate = Require-File @('src\Core\Migration.ControlPlane\Services\RunPreflightGateService.cs')
Require-Text $gate '"RequirePreflightGate"'
Require-Text $gate 'Preflight gate is advisory by default'
Reject-Text $gate 'A successful preflight is required before starting a non-dry-run migration.'

$apiPreflight = Require-File @('src\Admin\Migration.Admin.Web\src\api\preflight.ts', 'Admin\Migration.Admin.Web\src\api\preflight.ts')
Require-Text $apiPreflight '/preflight/run'
Reject-Text $apiPreflight '}/preflight`,'

$page = Require-File @('src\Admin\Migration.Admin.Web\src\features\operations\preflight\pages\Preflight.tsx', 'Admin\Migration.Admin.Web\src\features\operations\preflight\pages\Preflight.tsx')
Require-Text $page 'Readiness is advisory'
Require-Text $page 'disabled={running}'
Reject-Text $page 'disabled={running || readinessStatus === "fail"}'
Reject-Text $page 'Fix failed readiness checks before running preflight.'

Write-Host 'P7 preflight advisory fix validation passed.'
