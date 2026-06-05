Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) { return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path }
    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

function Add-Check {
    param([System.Collections.Generic.List[string]]$Lines, [string]$Name, [bool]$Passed, [string]$Detail)
    $status = if ($Passed) { 'PASS' } else { 'CHECK' }
    $Lines.Add(('- {0}: {1} — {2}' -f $status, $Name, $Detail)) | Out-Null
}

function Test-FileContains {
    param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { return $false }
    $content = Get-Content -LiteralPath $path -Raw
    return ($null -ne $content -and $content.Contains($Text))
}

function Find-FileContaining {
    param([string]$RootPath, [string[]]$RequiredText)
    $files = Get-ChildItem -Path (Join-Path $RootPath 'src') -Filter '*.cs' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $matched = $true
        foreach ($text in $RequiredText) {
            if ($null -eq $content -or -not $content.Contains($text)) { $matched = $false; break }
        }
        if ($matched) { return $file.FullName.Substring($RootPath.Length).TrimStart('\', '/') }
    }
    return $null
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p7\P7.10B-P7-Closeout-Checklist.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) { New-Item -ItemType Directory -Path $outputFolder | Out-Null }

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P7.10B P7 Closeout Checklist') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This checklist captures the minimum operational criteria for closing P7 and moving into cloud deployment/live execution work.') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('## Automated repository checks') | Out-Null
$lines.Add('') | Out-Null

Add-Check -Lines $lines -Name 'SQL worker host exists' -Passed (Test-Path -LiteralPath (Join-Path $root 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj')) -Detail 'Dedicated SQL operational worker host project is present.'
Add-Check -Lines $lines -Name 'Worker uses runnable-run discovery' -Passed (Test-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Text 'GetRunnableRunIdsAsync') -Detail 'Worker can discover SQL runs without a RunId secret.'
Add-Check -Lines $lines -Name 'RunId is debug override only' -Passed (Test-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Text 'RunIdOverride') -Detail 'Logs expose RunIdOverride so pinned runs are visible.'
Add-Check -Lines $lines -Name 'SQL run coordinator discovery method exists' -Passed (Test-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinator.cs' -Text 'GetRunnableRunIdsAsync') -Detail 'Coordinator exposes runnable run lookup.'
Add-Check -Lines $lines -Name 'Execution-history writer exists' -Passed (Test-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs' -Text 'AttemptsTableName') -Detail 'SQL execution-history writer is options-driven.'
Add-Check -Lines $lines -Name 'SQL runtime bootstrap exists' -Passed (Test-Path -LiteralPath (Join-Path $root 'database\sql\p7\006_sql_operational_runtime_bootstrap_compatibility.sql')) -Detail 'Consolidated runtime compatibility bootstrap script is present.'
Add-Check -Lines $lines -Name 'Execution-history SQL exists' -Passed (Test-Path -LiteralPath (Join-Path $root 'database\sql\p7\007_sql_operational_execution_history.sql')) -Detail 'Execution-history schema script is present.'

$contractFile = Find-FileContaining -RootPath $root -RequiredText @('interface IOperationalWorkItemQueue', 'OperationalWorkItemRecord', 'OperationalWorkItemRunSummary')
Add-Check -Lines $lines -Name 'Work item contracts located' -Passed ($null -ne $contractFile) -Detail $(if ($null -ne $contractFile) { $contractFile } else { 'Not found.' })

$lines.Add('') | Out-Null
$lines.Add('## Required manual closeout commands') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('Run these from repository root before declaring P7 closed:') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('```powershell') | Out-Null
$lines.Add('powershell -ExecutionPolicy Bypass -File .\scripts\Validate-P710BP7CloseoutReadiness.ps1') | Out-Null
$lines.Add('dotnet build') | Out-Null
$lines.Add('dotnet user-secrets remove "SqlOperationalQueueExecutor:RunId" --project .\src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj') | Out-Null
$lines.Add('dotnet run --project .\src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj --environment Development') | Out-Null
$lines.Add('```') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('Expected local idle output when no queued runs exist:') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('```text') | Out-Null
$lines.Add('RunIdOverride=(null)') | Out-Null
$lines.Add('SQL operational worker host readiness check passed. Status=Ready') | Out-Null
$lines.Add('SQL operational queue idle. No runnable migration runs found.') | Out-Null
$lines.Add('```') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('## Local development secrets baseline') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('Keep only these SQL operational worker secrets locally:') | Out-Null
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
$lines.Add('Do not configure `SqlOperationalQueueExecutor:RunId` except as a short-lived debug override.') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('## P7 closeout recommendation') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('If the validation script passes, `dotnet build` succeeds, and the worker reaches the expected no-RunId idle state, P7 can be considered operationally closed. The next phase should focus on cloud deployment, live run creation, telemetry, and real connector execution rather than more runtime scaffolding.') | Out-Null

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
