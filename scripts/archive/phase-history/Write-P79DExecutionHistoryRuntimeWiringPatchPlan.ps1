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

function Add-FileProbe {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$RelativePath,
        [string[]]$Needles
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

    foreach ($needle in $Needles) {
        if ($null -ne $content -and $content.Contains($needle)) {
            $Lines.Add(('- Contains: `{0}`' -f $needle)) | Out-Null
        }
        else {
            $Lines.Add(('- Missing: `{0}`' -f $needle)) | Out-Null
        }
    }

    $Lines.Add('') | Out-Null
}

function Add-SearchProbe {
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
        $Lines.Add(('- {0}:{1}: {2}' -f $relative, $match.LineNumber, $match.Line.Trim())) | Out-Null
        $count++
    }

    if ($count -eq 0) {
        $Lines.Add('- No matches found.') | Out-Null
    }

    $Lines.Add('') | Out-Null
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p7\P7.9D-Execution-History-Runtime-Wiring-Patch-Plan.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P7.9D Execution History Runtime Wiring Patch Plan') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This generated inventory identifies the exact current repo symbols that P7.9E should wire for execution-history recording.') | Out-Null
$lines.Add('') | Out-Null

Add-FileProbe -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Needles @('ExecuteClaimedItemAsync', 'ISqlOperationalWorkItemExecutor', 'CompleteOperationalWorkItemRequest', 'FailOperationalWorkItemRequest')
Add-FileProbe -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalMigrationJobWorkItemExecutor.cs' -Needles @('ExecuteAsync', 'Succeeded', 'ErrorCode', 'ResultJson')
Add-FileProbe -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs' -Needles @('AttemptsTableName', 'ExecuteAsync', 'OpenConnection')
Add-FileProbe -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\WorkItems\IOperationalWorkItemQueue.cs' -Needles @('OperationalWorkItemRecord', 'OperationalWorkItemRunSummary')

Add-SearchProbe -Lines $lines -RootPath $root -Title 'Execution history symbols' -Pattern 'ExecutionHistory'
Add-SearchProbe -Lines $lines -RootPath $root -Title 'Execution history writer symbols' -Pattern 'SqlOperationalExecutionHistoryWriter'
Add-SearchProbe -Lines $lines -RootPath $root -Title 'Work item executor symbols' -Pattern 'ISqlOperationalWorkItemExecutor'
Add-SearchProbe -Lines $lines -RootPath $root -Title 'Work item execution result symbols' -Pattern 'SqlOperationalWorkItemExecutionResult'

$lines.Add('## P7.9E wiring target') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('P7.9E should wire execution-history recording at the boundary where `SqlOperationalWorkItemWorker` calls `ISqlOperationalWorkItemExecutor.ExecuteAsync`, because that boundary has the claimed work item, start/end time, success/failure result, retry metadata, and final result/error payload.') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('The preferred implementation should avoid changing package references and should keep the SQL writer options-driven.') | Out-Null

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
