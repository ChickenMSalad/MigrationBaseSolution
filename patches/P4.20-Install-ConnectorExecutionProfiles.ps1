[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$payloadRoot = Join-Path $repoRoot 'payload'
Write-Host "[P4.20] Repo root: $repoRoot"

function Copy-PayloadFile {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath
    )

    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath
    $targetDirectory = Split-Path -Parent $target

    if (-not (Test-Path -LiteralPath $source)) {
        throw "Payload source not found: $source"
    }

    if (-not $Apply) {
        Write-Host "[P4.20] WOULD copy $RelativePath"
        return
    }

    if (-not (Test-Path -LiteralPath $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    if ($PSCmdlet.ShouldProcess($target, 'Copy payload file')) {
        Copy-Item -LiteralPath $source -Destination $target -Force
        Write-Host "[P4.20] Copied $RelativePath"
    }
}

function Add-LineIfMissing {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Line,
        [Parameter(Mandatory = $true)][string]$BeforePattern
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "File not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Line)) {
        Write-Host "[P4.20] Already present: $Line"
        return
    }

    if (-not $Apply) {
        Write-Host ("[P4.20] WOULD add line to {0}: {1}" -f $Path, $Line)
        return
    }

    $updated = [regex]::Replace($content, $BeforePattern, [System.Text.RegularExpressions.MatchEvaluator]{
        param($match)
        return $Line + [Environment]::NewLine + $match.Value
    }, 1)

    if ($updated -eq $content) {
        $updated = $content.TrimEnd() + [Environment]::NewLine + $Line + [Environment]::NewLine
    }

    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Host "[P4.20] Updated $Path"
}

$files = @(
    'src/Core/Migration.Admin.Api/Endpoints/Operational/Connectors/OperationalConnectorExecutionProfileEndpointExtensions.cs',
    'apps/migration-admin-ui/src/features/executionProfiles/executionProfileTypes.ts',
    'apps/migration-admin-ui/src/features/executionProfiles/executionProfileApi.ts',
    'apps/migration-admin-ui/src/features/executionProfiles/ExecutionProfileWorkspace.tsx',
    'docs/operations/P4.20-connector-execution-profiles-runtime-policies.md'
)

foreach ($file in $files) {
    Copy-PayloadFile -RelativePath $file
}

$programPath = Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Program.cs'
Add-LineIfMissing -Path $programPath -Line 'using Migration.Admin.Api.Endpoints.Operational.Connectors;' -BeforePattern 'using '

$programText = Get-Content -LiteralPath $programPath -Raw
if (-not $programText.Contains('MapOperationalConnectorExecutionProfileEndpoints')) {
    if (-not $Apply) {
        Write-Host '[P4.20] WOULD add MapOperationalConnectorExecutionProfileEndpoints endpoint registration'
    }
    else {
        $programText = [regex]::Replace($programText, '(app\.Run\s*\(\s*\)\s*;)', 'app.MapOperationalConnectorExecutionProfileEndpoints();' + [Environment]::NewLine + '$1', 1)
        Set-Content -LiteralPath $programPath -Value $programText -Encoding UTF8
        Write-Host '[P4.20] Added MapOperationalConnectorExecutionProfileEndpoints endpoint registration'
    }
}

$appPath = Join-Path $repoRoot 'apps/migration-admin-ui/src/App.tsx'
if (Test-Path -LiteralPath $appPath) {
    Add-LineIfMissing -Path $appPath -Line "import { ExecutionProfileWorkspace } from './features/executionProfiles/ExecutionProfileWorkspace';" -BeforePattern 'import '

    $appText = Get-Content -LiteralPath $appPath -Raw
    if (-not $appText.Contains('<ExecutionProfileWorkspace />')) {
        if (-not $Apply) {
            Write-Host '[P4.20] WOULD add ExecutionProfileWorkspace to App.tsx'
        }
        else {
            $appText = $appText -replace '(</main>)', ("  <ExecutionProfileWorkspace />" + [Environment]::NewLine + '$1')
            Set-Content -LiteralPath $appPath -Value $appText -Encoding UTF8
            Write-Host '[P4.20] Added ExecutionProfileWorkspace to App.tsx'
        }
    }
}

Write-Host '[P4.20] Complete.'
