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

function Assert-PathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required path missing: $RelativePath" }
}

function Assert-NoPathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (Test-Path -LiteralPath $path) { throw "Invalid path should not exist: $RelativePath" }
}

function Assert-FileContains {
    param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required file missing: $RelativePath" }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) { throw "Required text missing from $RelativePath : $Text" }
}

function Assert-AnySourceContains {
    param([string]$RootPath, [string]$UnderRelativePath, [string[]]$RequiredTexts, [string]$Description)
    $searchRoot = Join-Path $RootPath $UnderRelativePath
    if (-not (Test-Path -LiteralPath $searchRoot)) { throw "Required search root missing: $UnderRelativePath" }
    $files = Get-ChildItem -Path $searchRoot -Filter '*.cs' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $allFound = $true
        foreach ($text in $RequiredTexts) {
            if ($null -eq $content -or -not $content.Contains($text)) { $allFound = $false; break }
        }
        if ($allFound) { return }
    }
    throw "Required source pattern not found for $Description under $UnderRelativePath"
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Infrastructure'
Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Worker'

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.3A-OpenTelemetry-Azure-Monitor-Baseline.md'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.3A-OpenTelemetry-Azure-Monitor-Baseline.md' -Text 'OpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.3A-OpenTelemetry-Azure-Monitor-Baseline.md' -Text 'Azure Monitor'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.3A-OpenTelemetry-Azure-Monitor-Baseline.md' -Text 'ServiceBusCorrelationId'

Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Core' -RequiredTexts @('OperationalExecutionTelemetryFields') -Description 'operational telemetry field constants'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Workers' -RequiredTexts @('BeginScope') -Description 'runtime correlation scopes'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor' -RequiredTexts @('ServiceBusCorrelationId') -Description 'Service Bus executor correlation scope'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Core\MigrationBase.Core\Cloud\Azure' -RequiredTexts @('CorrelationId') -Description 'Azure correlation/observability surface'

Write-Host 'P8.3A OpenTelemetry/Azure Monitor baseline validation passed.'
