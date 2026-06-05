Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }

    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }

    return (Get-Location).Path
}

$root = Get-RepositoryRoot
$templatePath = Join-Path $root 'deploy\azure\container-apps\p8.1h-container-apps-settings.template.json'

if (-not (Test-Path -LiteralPath $templatePath)) {
    throw "Required template missing: deploy\azure\container-apps\p8.1h-container-apps-settings.template.json"
}

$original = Get-Content -LiteralPath $templatePath
$filtered = New-Object 'System.Collections.Generic.List[string]'

foreach ($line in $original) {
    if ($line.Contains('MIGRATION_SqlOperationalQueueExecutor__RunId')) {
        continue
    }

    $filtered.Add($line) | Out-Null
}

Set-Content -LiteralPath $templatePath -Value $filtered -Encoding UTF8
Write-Host "Removed debug-only MIGRATION_SqlOperationalQueueExecutor__RunId from deploy\azure\container-apps\p8.1h-container-apps-settings.template.json"
