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
    param([string] $Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

function Add-FileSummary {
    param([System.Collections.Generic.List[string]] $Lines, [string] $RootPath, [string] $RelativePath, [string[]] $Patterns)
    $Lines.Add('') | Out-Null
    $Lines.Add(('## {0}' -f $RelativePath)) | Out-Null
    $Lines.Add('') | Out-Null
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $Lines.Add('Missing.') | Out-Null
        return
    }
    $Lines.Add('Present.') | Out-Null
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            $Lines.Add(('- Contains: `{0}`' -f $pattern)) | Out-Null
        }
        else {
            $Lines.Add(('- Missing: `{0}`' -f $pattern)) | Out-Null
        }
    }
}

function Add-SearchSection {
    param([System.Collections.Generic.List[string]] $Lines, [string] $RootPath, [string] $Title, [string] $SearchRoot, [string] $Pattern)
    $Lines.Add('') | Out-Null
    $Lines.Add(('## {0}' -f $Title)) | Out-Null
    $Lines.Add('') | Out-Null
    $searchRootPath = Join-Path $RootPath $SearchRoot
    if (-not (Test-Path -LiteralPath $searchRootPath)) {
        $Lines.Add(('Missing search root: {0}' -f $SearchRoot)) | Out-Null
        return
    }
    $matches = New-Object System.Collections.Generic.List[string]
    $files = Get-ChildItem -Path $searchRootPath -Include '*.cs','*.csproj','*.props','*.json','*.md' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($RootPath.Length + 1)
        $contentLines = Get-Content -LiteralPath $file.FullName
        for ($i = 0; $i -lt $contentLines.Length; $i++) {
            $line = $contentLines[$i]
            if ($line -like ('*{0}*' -f $Pattern)) {
                $lineNumber = $i + 1
                $matches.Add(('- {0}:{1}: {2}' -f $relative, $lineNumber, $line.Trim())) | Out-Null
            }
        }
    }
    if ($matches.Count -eq 0) {
        $Lines.Add('No matches found.') | Out-Null
    }
    else {
        foreach ($match in $matches) { $Lines.Add($match) | Out-Null }
    }
}

$root = Get-RepositoryRoot
$out = Join-Path $root 'docs\p8\P8.4D-OpenTelemetry-Runtime-Verification-Inventory.generated.md'
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# P8.4D OpenTelemetry Runtime Verification Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:o}' -f [DateTimeOffset]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory verifies OpenTelemetry runtime registration and smoke-test readiness.') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs' -Patterns @('AddOperationalOpenTelemetry','AddOpenTelemetry','AddSource','AddAzureMonitorTraceExporter')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' -Patterns @('Migration.Operational.Execution','ActivitySource')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Patterns @('AddOperationalOpenTelemetry','AddEnvironmentVariables(prefix: "MIGRATION_")')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' -Patterns @('AddOperationalOpenTelemetry')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Patterns @('AddOperationalOpenTelemetry')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'config\templates\p8.4d-local-otel-smoke-settings.template.json' -Patterns @('EnableTracing','EnableAzureMonitorExporter','TraceSamplingRatio')

Add-SearchSection -Lines $lines -RootPath $root -Title 'OpenTelemetry runtime registrations' -SearchRoot 'src' -Pattern 'AddOperationalOpenTelemetry'
Add-SearchSection -Lines $lines -RootPath $root -Title 'ActivitySource usage' -SearchRoot 'src' -Pattern 'OperationalExecutionActivitySources.Name'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Azure Monitor exporter usage' -SearchRoot 'src' -Pattern 'AddAzureMonitorTraceExporter'

$directory = Split-Path -Parent $out
if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host "Wrote $out"
