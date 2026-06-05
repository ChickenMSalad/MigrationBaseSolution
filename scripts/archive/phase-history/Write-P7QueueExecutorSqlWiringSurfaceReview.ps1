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

function Convert-ToRepoRelativePath {
    param([string]$RootPath, [string]$FullPath)

    $rootWithSlash = $RootPath.TrimEnd('\') + '\'
    if ($FullPath.StartsWith($rootWithSlash, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $FullPath.Substring($rootWithSlash.Length)
    }

    return $FullPath
}

function Add-Line {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Text
    )

    [void]$Lines.Add($Text)
}

function Add-FileListSection {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Title,
        [string]$RootPath,
        [string]$RelativeFolder,
        [string[]]$Extensions
    )

    Add-Line -Lines $Lines -Text "## $Title"
    Add-Line -Lines $Lines -Text ''

    $folder = Join-Path $RootPath $RelativeFolder
    if (-not (Test-Path -LiteralPath $folder)) {
        Add-Line -Lines $Lines -Text "Missing folder: ``$RelativeFolder``"
        Add-Line -Lines $Lines -Text ''
        return
    }

    $files = Get-ChildItem -Path $folder -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) -and $Extensions -contains $_.Extension } |
        Sort-Object FullName

    if ($null -eq $files -or @($files).Count -eq 0) {
        Add-Line -Lines $Lines -Text '_No matching files found._'
        Add-Line -Lines $Lines -Text ''
        return
    }

    foreach ($file in @($files)) {
        $relative = Convert-ToRepoRelativePath -RootPath $RootPath -FullPath $file.FullName
        Add-Line -Lines $Lines -Text ('- `' + $relative + '`')
    }

    Add-Line -Lines $Lines -Text ''
}

function Add-SearchSection {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Title,
        [string]$RootPath,
        [string[]]$RelativeFolders,
        [string[]]$Terms
    )

    Add-Line -Lines $Lines -Text "## $Title"
    Add-Line -Lines $Lines -Text ''

    $matchedAny = $false

    foreach ($relativeFolder in $RelativeFolders) {
        $folder = Join-Path $RootPath $relativeFolder
        if (-not (Test-Path -LiteralPath $folder)) {
            continue
        }

        $files = Get-ChildItem -Path $folder -File -Recurse |
            Where-Object { -not (Test-IsIgnoredPath $_.FullName) -and $_.Extension -in @('.cs', '.csproj', '.json', '.sql') }

        foreach ($file in @($files)) {
            $content = Get-Content -Path $file.FullName -Raw
            if ($null -eq $content) {
                continue
            }

            $foundTerms = @()
            foreach ($term in $Terms) {
                if ($content.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                    $foundTerms += $term
                }
            }

            if (@($foundTerms).Count -gt 0) {
                $matchedAny = $true
                $relative = Convert-ToRepoRelativePath -RootPath $RootPath -FullPath $file.FullName
                Add-Line -Lines $Lines -Text ('- `' + $relative + '` => ' + (($foundTerms | Sort-Object -Unique) -join ', '))
            }
        }
    }

    if (-not $matchedAny) {
        Add-Line -Lines $Lines -Text '_No matching terms found._'
    }

    Add-Line -Lines $Lines -Text ''
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p7\P7.6B-QueueExecutor-SQL-Wiring-Surface-Review.generated.md'
$outputFolder = Split-Path -Parent $outputPath

if (-not (Test-Path -LiteralPath $outputFolder)) {
    New-Item -Path $outputFolder -ItemType Directory -Force | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
Add-Line -Lines $lines -Text '# P7.6B QueueExecutor SQL Wiring Surface Review'
Add-Line -Lines $lines -Text ''
Add-Line -Lines $lines -Text ('Generated: ' + (Get-Date).ToString('u'))
Add-Line -Lines $lines -Text ''
Add-Line -Lines $lines -Text 'This report is used to place the next implementation set into existing repo-native runtime surfaces only.'
Add-Line -Lines $lines -Text ''

Add-FileListSection -Lines $lines -Title 'QueueExecutor files' -RootPath $root -RelativeFolder 'src\Workers\Migration.Workers.QueueExecutor' -Extensions @('.cs', '.csproj', '.json')
Add-FileListSection -Lines $lines -Title 'Infrastructure SQL files' -RootPath $root -RelativeFolder 'src\Core\Migration.Infrastructure.Sql' -Extensions @('.cs', '.csproj', '.json')
Add-FileListSection -Lines $lines -Title 'Orchestration files' -RootPath $root -RelativeFolder 'src\Core\Migration.Orchestration' -Extensions @('.cs', '.csproj', '.json')
Add-FileListSection -Lines $lines -Title 'GenericRuntime files' -RootPath $root -RelativeFolder 'src\Core\Migration.GenericRuntime' -Extensions @('.cs', '.csproj', '.json')
Add-FileListSection -Lines $lines -Title 'SQL Operational Worker host files' -RootPath $root -RelativeFolder 'src\Hosts\Migration.Hosts.SqlOperationalWorker' -Extensions @('.cs', '.csproj', '.json')

Add-SearchSection -Lines $lines -Title 'Runtime and queue terms' -RootPath $root -RelativeFolders @('src\Workers\Migration.Workers.QueueExecutor', 'src\Core\Migration.Infrastructure.Sql', 'src\Core\Migration.Orchestration', 'src\Core\Migration.GenericRuntime', 'src\Hosts\Migration.Hosts.SqlOperationalWorker') -Terms @('Queue', 'Execute', 'Executor', 'Run', 'WorkItem', 'Retry', 'Failure', 'Lease', 'Heartbeat', 'Manifest')

Add-Line -Lines $lines -Text '## Placement decision for next implementation set'
Add-Line -Lines $lines -Text ''
Add-Line -Lines $lines -Text '- SQL persistence code should live under `src\Core\Migration.Infrastructure.Sql`.'
Add-Line -Lines $lines -Text '- Queue execution glue should live under `src\Workers\Migration.Workers.QueueExecutor`.'
Add-Line -Lines $lines -Text '- Host startup code should live under `src\Hosts\Migration.Hosts.SqlOperationalWorker`.'
Add-Line -Lines $lines -Text '- Do not create `Migration.Infrastructure.Runtime` or top-level `src\Migration.*` projects.'

Set-Content -Path $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
