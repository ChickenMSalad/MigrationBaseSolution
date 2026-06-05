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

function Add-SearchSection {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$Title,
        [string]$Pattern
    )

    $Lines.Add("## $Title") | Out-Null
    $Lines.Add('') | Out-Null

    $matches = Get-ChildItem -Path (Join-Path $RootPath 'src') -Filter '*.cs' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) } |
        Select-String -Pattern $Pattern -SimpleMatch

    $count = 0
    foreach ($match in $matches) {
        $relative = $match.Path.Substring($RootPath.Length).TrimStart('\', '/')
        $Lines.Add(("- {0}:{1}: {2}" -f $relative, $match.LineNumber, $match.Line.Trim())) | Out-Null
        $count++
    }

    if ($count -eq 0) {
        $Lines.Add('- No matches found.') | Out-Null
    }

    $Lines.Add('') | Out-Null
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p8\P8.1C-Cloud-Secret-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P8.1C Cloud Secret Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures cloud secret and app-setting surfaces for the SQL operational worker host.') | Out-Null
$lines.Add('') | Out-Null

$lines.Add('## Required Azure app settings') | Out-Null
$lines.Add('') | Out-Null
$settings = @(
    'MIGRATION_ConnectionStrings__MigrationOperationalStore',
    'MIGRATION_SqlOperationalQueueExecutor__Enabled',
    'MIGRATION_SqlOperationalQueueExecutor__WorkerId',
    'MIGRATION_SqlOperationalQueueExecutor__BatchSize',
    'MIGRATION_SqlOperationalQueueExecutor__LeaseSeconds',
    'MIGRATION_SqlOperationalQueueExecutor__PollDelaySeconds',
    'MIGRATION_SqlOperationalQueueExecutor__RunUntilIdleAndStop',
    'MIGRATION_SqlOperationalMigrationJobExecutor__Enabled'
)
foreach ($setting in $settings) {
    $lines.Add("- ``$setting``") | Out-Null
}
$lines.Add('') | Out-Null

$lines.Add('## Debug-only setting') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('- `MIGRATION_SqlOperationalQueueExecutor__RunId` must not be configured outside short-lived debug runs.') | Out-Null
$lines.Add('') | Out-Null

Add-SearchSection -Lines $lines -RootPath $root -Title 'Environment variable providers' -Pattern 'AddEnvironmentVariables'
Add-SearchSection -Lines $lines -RootPath $root -Title 'MigrationOperationalStore connection string usage' -Pattern 'MigrationOperationalStore'
Add-SearchSection -Lines $lines -RootPath $root -Title 'SqlOperationalQueueExecutor options usage' -Pattern 'SqlOperationalQueueExecutor'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Key Vault package/provider references' -Pattern 'KeyVault'

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
