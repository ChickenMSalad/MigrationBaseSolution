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
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

function Assert-AnySourceContains {
    param([string]$RootPath, [string]$UnderRelativePath, [string[]]$RequiredTexts, [string]$Description)
    $folder = Join-Path $RootPath $UnderRelativePath
    if (-not (Test-Path -LiteralPath $folder)) { throw "Required search root missing: $UnderRelativePath" }

    $files = Get-ChildItem -Path $folder -Filter '*.cs' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
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

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.2A-ServiceBus-Execution-Readiness.md'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor'

Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher' -RequiredTexts @('BackgroundService', 'SqlServiceBusDispatcher') -Description 'Service Bus dispatcher worker'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor' -RequiredTexts @('BackgroundService', 'SqlServiceBusExecutor') -Description 'Service Bus executor worker'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor' -RequiredTexts @('IServiceBusWorkItemExecutor') -Description 'Service Bus work item executor abstraction'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor' -RequiredTexts @('ServiceBusWorkItemMessage') -Description 'Service Bus work item message contract'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Core' -RequiredTexts @('ServiceBusExecutor') -Description 'Azure/cloud role or operational reference for Service Bus executor'

Write-Host 'P8.2A Service Bus execution readiness validation passed.'
