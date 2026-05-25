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
    $Lines.Add(('## {0}' -f $RelativePath)) | Out-Null
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
            $Lines.Add(('- Contains: `{0}`' -f $pattern)) | Out-Null
        } else {
            $Lines.Add(('- Missing: `{0}`' -f $pattern)) | Out-Null
        }
    }
}

function Add-SearchSection {
    param([string]$Title, [string]$SearchRoot, [string]$Pattern)
    $Lines.Add('') | Out-Null
    $Lines.Add(('## {0}' -f $Title)) | Out-Null
    $Lines.Add('') | Out-Null
    $searchPath = Join-Path $root $SearchRoot
    if (-not (Test-Path -LiteralPath $searchPath)) {
        $Lines.Add(('Missing search root: {0}' -f $SearchRoot)) | Out-Null
        return
    }
    $matches = New-Object System.Collections.Generic.List[string]
    $files = Get-ChildItem -Path $searchPath -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($root.Length + 1)
        $contentLines = Get-Content -LiteralPath $file.FullName -ErrorAction Stop
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
    } else {
        foreach ($match in $matches) { $Lines.Add($match) | Out-Null }
    }
}

$root = Get-RepositoryRoot
$out = Join-Path $root 'docs\p9\P9B-Azure-Environment-Resource-Inventory.generated.md'
$Lines = New-Object System.Collections.Generic.List[string]
$Lines.Add('# P9B Azure Environment / Resource Inventory') | Out-Null
$Lines.Add('') | Out-Null
$Lines.Add(('GeneratedUtc: {0}' -f [DateTimeOffset]::UtcNow.ToString('o'))) | Out-Null
$Lines.Add('') | Out-Null
$Lines.Add('This inventory captures cloud execution resource/settings readiness before concrete Azure deployment configuration is finalized.') | Out-Null

Add-FileSummary -RelativePath 'docs\p9\P9B-Azure-Environment-Resource-Inventory.md' -Patterns @('Azure SQL', 'Service Bus', 'Application Insights')
Add-FileSummary -RelativePath 'config\templates\p9b-azure-resource-inventory.template.json' -Patterns @('ServiceBusNamespace', 'AzureMonitorConnectionString', 'MigrationOperationalStore')
Add-FileSummary -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs' -Patterns @('AddOpenTelemetry', 'AddSource', 'AddAzureMonitorTraceExporter')
Add-FileSummary -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Patterns @('AddOperationalOpenTelemetry', 'AddEnvironmentVariables(prefix: "MIGRATION_")')
Add-FileSummary -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' -Patterns @('AddOperationalOpenTelemetry')
Add-FileSummary -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Patterns @('AddOperationalOpenTelemetry')

Add-SearchSection -Title 'Service Bus configuration references' -SearchRoot 'src' -Pattern 'ServiceBus'
Add-SearchSection -Title 'Operational store connection references' -SearchRoot 'src' -Pattern 'MigrationOperationalStore'
Add-SearchSection -Title 'Azure Monitor / Application Insights references' -SearchRoot 'src' -Pattern 'ApplicationInsights'
Add-SearchSection -Title 'MIGRATION_ environment provider references' -SearchRoot 'src' -Pattern 'MIGRATION_'

$directory = Split-Path -Parent $out
if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
Set-Content -LiteralPath $out -Value $Lines -Encoding UTF8
Write-Host "Wrote $out"
