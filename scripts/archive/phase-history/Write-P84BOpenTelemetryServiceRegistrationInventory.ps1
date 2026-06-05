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
    param([string]$RelativePath, [string[]]$Patterns)
    $Lines.Add('') | Out-Null
    $Lines.Add(("## {0}" -f $RelativePath)) | Out-Null
    $Lines.Add('') | Out-Null
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $Lines.Add('Missing.') | Out-Null
        return
    }
    $Lines.Add('Present.') | Out-Null
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            $Lines.Add(("- Contains: ``{0}``" -f $pattern)) | Out-Null
        }
        else {
            $Lines.Add(("- Missing: ``{0}``" -f $pattern)) | Out-Null
        }
    }
}

function Add-SearchSection {
    param([string]$Title, [string]$SearchRoot, [string]$Pattern)
    $Lines.Add('') | Out-Null
    $Lines.Add(("## {0}" -f $Title)) | Out-Null
    $Lines.Add('') | Out-Null
    $searchRootPath = Join-Path $root $SearchRoot
    if (-not (Test-Path -LiteralPath $searchRootPath)) {
        $Lines.Add(("Missing search root: {0}" -f $SearchRoot)) | Out-Null
        return
    }
    $matches = New-Object System.Collections.Generic.List[string]
    $files = Get-ChildItem -Path $searchRootPath -Filter '*.cs' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($root.Length + 1)
        $contentLines = Get-Content -LiteralPath $file.FullName
        for ($i = 0; $i -lt $contentLines.Length; $i++) {
            $line = $contentLines[$i]
            if ($line -like ("*{0}*" -f $Pattern)) {
                $lineNumber = $i + 1
                $matches.Add(("- {0}:{1}: {2}" -f $relative, $lineNumber, $line.Trim())) | Out-Null
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
$outDir = Join-Path $root 'docs\p8'
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$out = Join-Path $outDir 'P8.4B-OpenTelemetry-Service-Registration-Inventory.generated.md'

$Lines = New-Object System.Collections.Generic.List[string]
$Lines.Add('# P8.4B OpenTelemetry Service Registration Inventory') | Out-Null
$Lines.Add('') | Out-Null
$Lines.Add(("GeneratedUtc: {0:o}" -f [DateTimeOffset]::UtcNow)) | Out-Null
$Lines.Add('') | Out-Null
$Lines.Add('This inventory captures the staged OpenTelemetry service registration configuration baseline before package/exporter wiring.') | Out-Null

Add-FileSummary -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryRegistrationOptions.cs' -Patterns @('OpenTelemetry','EnableTracing','EnableAzureMonitorExporter','AzureMonitorConnectionString','TraceSamplingRatio')
Add-FileSummary -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryConfigurationKeys.cs' -Patterns @('ApplicationInsights:ConnectionString','APPLICATIONINSIGHTS_CONNECTION_STRING','OpenTelemetry:AzureMonitorConnectionString')
Add-FileSummary -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' -Patterns @('Migration.Operational.Execution','ActivitySource')
Add-FileSummary -RelativePath 'config\templates\p8.4b-opentelemetry-registration-settings.template.json' -Patterns @('OpenTelemetry','AzureMonitorConnectionString','EnableAzureMonitorExporter')

Add-SearchSection -Title 'OpenTelemetry references' -SearchRoot 'src' -Pattern 'OpenTelemetry'
Add-SearchSection -Title 'Application Insights references' -SearchRoot 'src' -Pattern 'ApplicationInsights'
Add-SearchSection -Title 'Azure Monitor references' -SearchRoot 'src' -Pattern 'AzureMonitor'
Add-SearchSection -Title 'ActivitySource references' -SearchRoot 'src' -Pattern 'ActivitySource'

Set-Content -LiteralPath $out -Value $Lines -Encoding UTF8
Write-Host ("Wrote {0}" -f $out)
