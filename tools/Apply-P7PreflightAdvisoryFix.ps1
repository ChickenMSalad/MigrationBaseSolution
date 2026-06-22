Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot
$packageRoot = Split-Path -Parent $scriptRoot
$filesRoot = Join-Path $packageRoot 'files'

function Copy-PatchFile {
    param(
        [Parameter(Mandatory=$true)][string]$SourceRelativePath,
        [Parameter(Mandatory=$true)][string[]]$TargetRelativePaths
    )

    $source = Join-Path $filesRoot $SourceRelativePath
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Package file missing: $source"
    }

    $copied = $false
    foreach ($targetRelativePath in $TargetRelativePaths) {
        $target = Join-Path $repoRoot $targetRelativePath
        $targetDir = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $targetDir)) {
            continue
        }

        if (Test-Path -LiteralPath $target) {
            $stamp = Get-Date -Format 'yyyyMMddHHmmss'
            Copy-Item -LiteralPath $target -Destination ($target + ".p7-preflight-advisory.$stamp.bak") -Force
        }

        Copy-Item -LiteralPath $source -Destination $target -Force
        Write-Host "Applied $targetRelativePath"
        $copied = $true
        break
    }

    if (-not $copied) {
        $joined = [string]::Join(', ', $TargetRelativePaths)
        throw "Could not find a valid target directory for: $joined"
    }
}

Copy-PatchFile `
    -SourceRelativePath 'src\Core\Migration.Admin.Api\Endpoints\System\OperationalHealthEndpointExtensions.cs' `
    -TargetRelativePaths @('src\Core\Migration.Admin.Api\Endpoints\System\OperationalHealthEndpointExtensions.cs')

Copy-PatchFile `
    -SourceRelativePath 'src\Core\Migration.ControlPlane\Services\RunPreflightGateService.cs' `
    -TargetRelativePaths @('src\Core\Migration.ControlPlane\Services\RunPreflightGateService.cs')

Copy-PatchFile `
    -SourceRelativePath 'src\Admin\Migration.Admin.Web\src\api\preflight.ts' `
    -TargetRelativePaths @('src\Admin\Migration.Admin.Web\src\api\preflight.ts', 'Admin\Migration.Admin.Web\src\api\preflight.ts')

Copy-PatchFile `
    -SourceRelativePath 'src\Admin\Migration.Admin.Web\src\features\operations\preflight\pages\Preflight.tsx' `
    -TargetRelativePaths @('src\Admin\Migration.Admin.Web\src\features\operations\preflight\pages\Preflight.tsx', 'Admin\Migration.Admin.Web\src\features\operations\preflight\pages\Preflight.tsx')

Write-Host 'P7 preflight advisory fix applied.'
