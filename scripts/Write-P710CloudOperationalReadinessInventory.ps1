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

    if ($count -eq 0) {
        $Lines.Add('- No matches found.') | Out-Null
    }

    $Lines.Add('') | Out-Null
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p7\P7.10-Cloud-Operational-Readiness-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P7.10 Cloud Operational Readiness Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures the repo-native operational runtime surfaces that should be stable before moving from P7 runtime wiring into cloud deployment and live execution work.') | Out-Null
$lines.Add('') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Patterns @('AddSqlOperationalRuntimeReadiness', 'AddSqlOperationalQueueExecutor', 'AddSqlOperationalMigrationJobWorkItemExecutor', 'AddHostedService')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('GetRunnableRunIdsAsync', 'RunIdOverride', 'ExecuteRunAsync', 'ExecuteClaimedItemAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinator.cs' -Patterns @('GetRunnableRunIdsAsync', 'EvaluateCompletionAsync', 'RunsTableName')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs' -Patterns @('AttemptsTableName', 'OpenConnection', 'ExecuteAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'database\sql\p7\006_sql_operational_runtime_bootstrap_compatibility.sql' -Patterns @('migration.Runs', 'migration.WorkItems', 'CompletionEvaluatedUtc')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'database\sql\p7\007_sql_operational_execution_history.sql' -Patterns @('Execution', 'Attempt', 'WorkItem')

Add-SearchSection -Lines $lines -RootPath $root -Title 'Runnable run discovery' -Pattern 'GetRunnableRunIdsAsync'
Add-SearchSection -Lines $lines -RootPath $root -Title 'RunId override references' -Pattern 'RunIdOverride'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Execution history writer references' -Pattern 'SqlOperationalExecutionHistoryWriter'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Operational queue executor options references' -Pattern 'SqlOperationalQueueExecutor'

$lines.Add('## Recommended Local Secrets Baseline') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('Keep only these local development secrets for the SQL operational worker host:') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('```json') | Out-Null
$lines.Add('{') | Out-Null
$lines.Add('  "ConnectionStrings:MigrationOperationalStore": "Server=(localdb)\\MSSQLLocalDB;Database=MigrationOperationalStore;Trusted_Connection=True;TrustServerCertificate=True",') | Out-Null
$lines.Add('  "SqlOperationalQueueExecutor:Enabled": "true",') | Out-Null
$lines.Add('  "SqlOperationalQueueExecutor:WorkerId": "local-sql-operational-worker-01",') | Out-Null
$lines.Add('  "SqlOperationalQueueExecutor:BatchSize": "10",') | Out-Null
$lines.Add('  "SqlOperationalQueueExecutor:LeaseSeconds": "300",') | Out-Null
$lines.Add('  "SqlOperationalQueueExecutor:PollDelaySeconds": "5",') | Out-Null
$lines.Add('  "SqlOperationalQueueExecutor:RunUntilIdleAndStop": "false",') | Out-Null
$lines.Add('  "SqlOperationalMigrationJobExecutor:Enabled": "false"') | Out-Null
$lines.Add('}') | Out-Null
$lines.Add('```') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('Do not configure SqlOperationalQueueExecutor:RunId except as a short-lived debug override.') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('## P7 Exit Criteria') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('- Worker host starts in Development with RunIdOverride=(null).') | Out-Null
$lines.Add('- Readiness reports Ready.') | Out-Null
$lines.Add('- Worker discovers runnable runs from SQL instead of secrets.') | Out-Null
$lines.Add('- Idle state logs no runnable migration runs found when the queue is empty.') | Out-Null
$lines.Add('- SQL bootstrap compatibility script applies cleanly.') | Out-Null
$lines.Add('- Execution history schema exists and validates.') | Out-Null
$lines.Add('- No repo-native path regressions: no top-level src\\Migration.Infrastructure or src\\Migration.Worker.') | Out-Null
$lines.Add('') | Out-Null

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
