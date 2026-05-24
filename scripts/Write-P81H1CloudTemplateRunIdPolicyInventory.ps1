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

function Test-IsIgnoredPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p8\P8.1H.1-Cloud-Template-RunId-Policy-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath

if (-not (Test-Path -LiteralPath $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P8.1H.1 Cloud Template RunId Policy Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory checks cloud deployment templates for the debug-only SQL operational RunId override.') | Out-Null
$lines.Add('') | Out-Null

$templatePath = Join-Path $root 'deploy\azure\container-apps\p8.1h-container-apps-settings.template.json'
$lines.Add('## Container Apps settings template') | Out-Null
$lines.Add('') | Out-Null

if (Test-Path -LiteralPath $templatePath) {
    $content = Get-Content -LiteralPath $templatePath -Raw
    $lines.Add('Present.') | Out-Null

    if ($null -ne $content -and $content.Contains('MIGRATION_SqlOperationalQueueExecutor__RunId')) {
        $lines.Add('- Contains debug-only RunId setting: yes') | Out-Null
    }
    else {
        $lines.Add('- Contains debug-only RunId setting: no') | Out-Null
    }

    foreach ($required in @(
        'MIGRATION_ConnectionStrings__MigrationOperationalStore',
        'MIGRATION_SqlOperationalQueueExecutor__Enabled',
        'MIGRATION_SqlOperationalQueueExecutor__WorkerId',
        'MIGRATION_SqlOperationalQueueExecutor__BatchSize',
        'MIGRATION_SqlOperationalQueueExecutor__LeaseSeconds',
        'MIGRATION_SqlOperationalQueueExecutor__PollDelaySeconds',
        'MIGRATION_SqlOperationalQueueExecutor__RunUntilIdleAndStop'
    )) {
        if ($null -ne $content -and $content.Contains($required)) {
            $lines.Add("- Contains: ``$required``") | Out-Null
        }
        else {
            $lines.Add("- Missing: ``$required``") | Out-Null
        }
    }
}
else {
    $lines.Add('Missing.') | Out-Null
}

$lines.Add('') | Out-Null
$lines.Add('## Policy') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('- `MIGRATION_SqlOperationalQueueExecutor__RunId` is debug-only.') | Out-Null
$lines.Add('- Cloud workers should discover runnable runs from SQL with `RunIdOverride=(null)`.') | Out-Null
$lines.Add('- Keep `SqlOperationalQueueExecutor:RunId` only in short-lived user-secrets/local debug sessions.') | Out-Null

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
