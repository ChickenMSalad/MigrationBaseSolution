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
            $Lines.Add(("- Contains: ``{0}``" -f $pattern)) | Out-Null
        }
        else {
            $Lines.Add(("- Missing: ``{0}``" -f $pattern)) | Out-Null
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
$outputPath = Join-Path $root 'docs\p8\P8.1A-Cloud-Hosting-Readiness-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) { New-Item -ItemType Directory -Path $outputFolder | Out-Null }

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P8.1A Cloud Hosting Readiness Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures the repo-native host, SQL runtime, worker, execution-history, and configuration surfaces that P8.1 should use for Azure/cloud hosting work.') | Out-Null
$lines.Add('') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Patterns @('AddSqlOperationalRuntimeReadiness','AddSqlOperationalQueueExecutor','AddSqlOperationalMigrationJobWorkItemExecutor','AddHostedService','AddUserSecrets','AddEnvironmentVariables')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Options\SqlOperationalQueueExecutorOptions.cs' -Patterns @('SectionName','Enabled','WorkerId','BatchSize','LeaseSeconds','PollDelaySeconds','RunId')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('RunIdOverride','GetRunnableRunIdsAsync','ExecuteRunAsync','ExecuteClaimedItemAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinator.cs' -Patterns @('GetRunnableRunIdsAsync','RunsTableName','EvaluateCompletionAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs' -Patterns @('AttemptsTableName','OpenConnection','ExecuteAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'database\sql\p7\006_sql_operational_runtime_bootstrap_compatibility.sql' -Patterns @('migration.Runs','migration.WorkItems','CompletionEvaluatedUtc')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'database\sql\p7\007_sql_operational_execution_history.sql' -Patterns @('Execution','Attempt','WorkItem')

Add-SearchSection -Lines $lines -RootPath $root -Title 'Hosted worker registrations' -Pattern 'AddHostedService'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Connection string usage' -Pattern 'MigrationOperationalStore'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Environment variable provider usage' -Pattern 'AddEnvironmentVariables'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Readiness service registrations/usages' -Pattern 'AddSqlOperationalRuntimeReadiness'

$lines.Add('## P8.1A Recommended Cloud Configuration Baseline') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('- Use managed identity / Key Vault / app settings for cloud secrets.') | Out-Null
$lines.Add('- Do not configure SqlOperationalQueueExecutor:RunId outside short-lived debug runs.') | Out-Null
$lines.Add('- Required cloud setting: ConnectionStrings:MigrationOperationalStore or equivalent provider-backed connection string.') | Out-Null
$lines.Add('- Required worker settings: SqlOperationalQueueExecutor:Enabled, WorkerId, BatchSize, LeaseSeconds, PollDelaySeconds, RunUntilIdleAndStop=false.') | Out-Null
$lines.Add('- Production should set SqlOperationalMigrationJobExecutor:Enabled only when connector/job execution configuration is ready.') | Out-Null
$lines.Add('') | Out-Null

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
