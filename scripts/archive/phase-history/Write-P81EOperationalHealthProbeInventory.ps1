Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

function Add-FileSummary {
    param([System.Collections.Generic.List[string]]$Lines, [string]$RootPath, [string]$RelativePath, [string[]]$Patterns)
    $path = Join-Path $RootPath $RelativePath
    $Lines.Add("## $RelativePath") | Out-Null
    $Lines.Add('') | Out-Null
    if (-not (Test-Path -LiteralPath $path)) { $Lines.Add('Missing.') | Out-Null; $Lines.Add('') | Out-Null; return }
    $Lines.Add('Present.') | Out-Null
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) { $Lines.Add("- Contains: ``$pattern``") | Out-Null }
        else { $Lines.Add("- Missing: ``$pattern``") | Out-Null }
    }
    $Lines.Add('') | Out-Null
}

function Add-SearchSection {
    param([System.Collections.Generic.List[string]]$Lines, [string]$RootPath, [string]$Title, [string]$Pattern)
    $Lines.Add("## $Title") | Out-Null
    $Lines.Add('') | Out-Null
    $matches = Get-ChildItem -Path (Join-Path $RootPath 'src') -Filter '*.cs' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) } | Select-String -Pattern $Pattern -SimpleMatch
    $count = 0
    foreach ($match in $matches) {
        $relative = $match.Path.Substring($RootPath.Length).TrimStart('\', '/')
        $Lines.Add(("- {0}:{1}: {2}" -f $relative, $match.LineNumber, $match.Line.Trim())) | Out-Null
        $count++
    }
    if ($count -eq 0) { $Lines.Add('- No matches found.') | Out-Null }
    $Lines.Add('') | Out-Null
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p8\P8.1E-Operational-Health-Probe-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) { New-Item -ItemType Directory -Path $outputFolder | Out-Null }

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P8.1E Operational Health Probe Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures repo-native health, readiness, startup, and cloud probe surfaces for the SQL operational worker host.') | Out-Null
$lines.Add('') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Patterns @('SqlOperationalWorkerStartupProbe', 'AddSqlOperationalRuntimeReadiness', 'AddEnvironmentVariables(prefix: "MIGRATION_")')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('RunIdOverride', 'GetRunnableRunIdsAsync', 'SQL operational queue idle')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'docs\p8\P8.1E-Operational-Health-Probe-Surfaces.md' -Patterns @('Liveness', 'Readiness', 'SqlOperationalQueueExecutor:RunId')

Add-SearchSection -Lines $lines -RootPath $root -Title 'Readiness services and contracts' -Pattern 'IOperationalRuntimeReadinessService'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Hosted service registrations' -Pattern 'AddHostedService'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Health endpoint surfaces' -Pattern 'MapHealthChecks'
Add-SearchSection -Lines $lines -RootPath $root -Title 'MIGRATION_ environment configuration' -Pattern 'AddEnvironmentVariables(prefix: "MIGRATION_")'

$lines.Add('## Recommended probe contract') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('- Liveness: platform process/container liveness.') | Out-Null
$lines.Add('- Readiness: SQL operational startup probe and readiness service report Ready.') | Out-Null
$lines.Add('- Idle with no runnable runs is healthy.') | Out-Null
$lines.Add('- Do not configure SqlOperationalQueueExecutor:RunId outside debug runs.') | Out-Null
$lines.Add('') | Out-Null

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
