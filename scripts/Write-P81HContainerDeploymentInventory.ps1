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
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$RelativePath,
        [string[]]$Patterns
    )

    $path = Join-Path $RootPath $RelativePath
    $Lines.Add("## $RelativePath") | Out-Null
    $Lines.Add('') | Out-Null

    if (-not (Test-Path -LiteralPath $path)) {
        $Lines.Add('Missing.') | Out-Null
        $Lines.Add('') | Out-Null
        return
    }

    $Lines.Add('Present.') | Out-Null
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            $Lines.Add("- Contains: ``$pattern``") | Out-Null
        }
        else {
            $Lines.Add("- Missing: ``$pattern``") | Out-Null
        }
    }
    $Lines.Add('') | Out-Null
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

    if ($count -eq 0) { $Lines.Add('- No matches found.') | Out-Null }
    $Lines.Add('') | Out-Null
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p8\P8.1H-Container-Deployment-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) { New-Item -ItemType Directory -Path $outputFolder | Out-Null }

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P8.1H Container Deployment Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures container/deployment topology files and repo-native hosting surfaces for P8.1H.') | Out-Null
$lines.Add('') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'deploy\docker\Dockerfile.admin-api.template' -Patterns @('Migration.Admin.Api.csproj', 'ASPNETCORE_URLS', 'EXPOSE 8080')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'deploy\docker\Dockerfile.sql-operational-worker.template' -Patterns @('Migration.Hosts.SqlOperationalWorker.csproj', 'dotnet/runtime')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'deploy\docker\Dockerfile.servicebus-dispatcher.template' -Patterns @('Migration.Workers.ServiceBusDispatcher.csproj')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'deploy\docker\Dockerfile.servicebus-executor.template' -Patterns @('Migration.Workers.ServiceBusExecutor.csproj')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'deploy\azure\container-apps\p8.1h-container-apps-settings.template.json' -Patterns @('MIGRATION_ConnectionStrings__MigrationOperationalStore', 'MIGRATION_SqlOperationalQueueExecutor__RunId')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'docs\p8\P8.1H-Container-Deployment-Topology.md' -Patterns @('/health/live', '/health/ready', 'Azure Container Apps')

Add-SearchSection -Lines $lines -RootPath $root -Title 'Admin API health/probe endpoint references' -Pattern '/health/ready'
Add-SearchSection -Lines $lines -RootPath $root -Title 'SQL operational worker host references' -Pattern 'Migration.Hosts.SqlOperationalWorker'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus worker host references' -Pattern 'ServiceBusExecutor'
Add-SearchSection -Lines $lines -RootPath $root -Title 'MIGRATION_ environment provider references' -Pattern 'AddEnvironmentVariables(prefix: "MIGRATION_")'

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
