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

function Assert-SourceContainsAny {
    param([string]$RootPath, [string]$UnderRelativePath, [string[]]$Patterns, [string]$Description)
    $folder = Join-Path $RootPath $UnderRelativePath
    if (-not (Test-Path -LiteralPath $folder)) { throw "Required folder missing: $UnderRelativePath" }
    $files = Get-ChildItem -Path $folder -Filter '*.cs' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($pattern in $Patterns) {
            if ($null -ne $content -and $content.Contains($pattern)) { return }
        }
    }
    throw "Required source pattern not found for $Description under $UnderRelativePath"
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Infrastructure'
Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Worker'

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.1E-Operational-Health-Probe-Surfaces.md'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.1E-Operational-Health-Probe-Surfaces.md' -Text 'Liveness'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.1E-Operational-Health-Probe-Surfaces.md' -Text 'Readiness'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.1E-Operational-Health-Probe-Surfaces.md' -Text 'SqlOperationalQueueExecutor:RunId'

Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'SqlOperationalWorkerStartupProbe'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'AddSqlOperationalRuntimeReadiness'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'AddEnvironmentVariables(prefix: "MIGRATION_")'

Assert-SourceContainsAny -RootPath $root -UnderRelativePath 'src\Core\Migration.Application\Operational\Readiness' -Patterns @('IOperationalRuntimeReadinessService') -Description 'operational readiness contract'
Assert-SourceContainsAny -RootPath $root -UnderRelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\Readiness' -Patterns @('SqlOperationalRuntimeReadiness') -Description 'SQL operational readiness implementation or registration'
Assert-SourceContainsAny -RootPath $root -UnderRelativePath 'src\Workers\Migration.Workers.QueueExecutor' -Patterns @('RunIdOverride', 'GetRunnableRunIdsAsync') -Description 'cloud-correct queue worker discovery'

Write-Host 'P8.1E operational health/probe surface validation passed.'
