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

    if ($count -eq 0) {
        $Lines.Add('- No matches found.') | Out-Null
    }

    $Lines.Add('') | Out-Null
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p7\P7.9C-Execution-History-Wiring-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P7.9C Execution History Wiring Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures the repo-native files that should be used for the P7.9D execution-history runtime wiring patch.') | Out-Null
$lines.Add('') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('ISqlOperationalWorkItemExecutor', 'ExecuteClaimedItemAsync', 'CompleteOperationalWorkItemRequest', 'FailOperationalWorkItemRequest')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalMigrationJobWorkItemExecutor.cs' -Patterns @('ISqlOperationalWorkItemExecutor', 'ExecuteAsync', 'Succeeded', 'ErrorCode', 'ErrorMessage')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs' -Patterns @('AttemptsTableName', 'OpenConnection', 'ExecuteAsync')

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\WorkItems\IOperationalWorkItemQueue.cs' -Patterns @('OperationalWorkItemRecord', 'OperationalWorkItemRunSummary', 'CompleteOperationalWorkItemRequest', 'FailOperationalWorkItemRequest')

Add-SearchSection -Lines $lines -RootPath $root -Title 'ISqlOperationalWorkItemExecutor registrations/usages' -Pattern 'ISqlOperationalWorkItemExecutor'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Execution history writer registrations/usages' -Pattern 'SqlOperationalExecutionHistoryWriter'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Work item completion/failure calls' -Pattern 'CompleteOperationalWorkItemRequest'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Work item failure calls' -Pattern 'FailOperationalWorkItemRequest'

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
