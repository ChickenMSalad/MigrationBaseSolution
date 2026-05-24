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

function Get-RelativePath {
    param(
        [string]$RootPath,
        [string]$FullPath
    )

    $root = $RootPath.TrimEnd('\', '/')
    if ($FullPath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $FullPath.Substring($root.Length).TrimStart('\', '/')
    }

    return $FullPath
}

function Add-Heading {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Text,
        [int]$Level
    )

    $prefix = '#' * $Level
    [void]$Lines.Add("")
    [void]$Lines.Add("$prefix $Text")
    [void]$Lines.Add("")
}

function Add-FileListSection {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$Title,
        [string]$RelativeFolder,
        [string[]]$Extensions
    )

    Add-Heading -Lines $Lines -Text $Title -Level 2

    $folder = Join-Path $RootPath $RelativeFolder
    if (-not (Test-Path -LiteralPath $folder)) {
        [void]$Lines.Add("_Missing folder:_ ``$RelativeFolder``")
        return
    }

    $files = Get-ChildItem -Path $folder -File -Recurse |
        Where-Object {
            -not (Test-IsIgnoredPath $_.FullName) -and
            ($Extensions -contains $_.Extension)
        } |
        Sort-Object FullName

    if ($null -eq $files -or @($files).Count -eq 0) {
        [void]$Lines.Add("_No matching files found._")
        return
    }

    foreach ($file in $files) {
        $relative = Get-RelativePath -RootPath $RootPath -FullPath $file.FullName
        [void]$Lines.Add(("- ``{0}``" -f $relative))
    }
}

function Add-SearchHitsSection {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$Title,
        [string]$RelativeFolder,
        [string[]]$Patterns,
        [string[]]$Extensions
    )

    Add-Heading -Lines $Lines -Text $Title -Level 2

    $folder = Join-Path $RootPath $RelativeFolder
    if (-not (Test-Path -LiteralPath $folder)) {
        [void]$Lines.Add("_Missing folder:_ ``$RelativeFolder``")
        return
    }

    $files = Get-ChildItem -Path $folder -File -Recurse |
        Where-Object {
            -not (Test-IsIgnoredPath $_.FullName) -and
            ($Extensions -contains $_.Extension)
        } |
        Sort-Object FullName

    $hitCount = 0

    foreach ($file in $files) {
        $content = Get-Content -Path $file.FullName -Raw
        if ($null -eq $content) {
            continue
        }

        foreach ($pattern in $Patterns) {
            if ($content.IndexOf($pattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $relative = Get-RelativePath -RootPath $RootPath -FullPath $file.FullName
                [void]$Lines.Add(("- ``{0}`` contains ``{1}``" -f $relative, $pattern))
                $hitCount++
                break
            }
        }
    }

    if ($hitCount -eq 0) {
        [void]$Lines.Add("_No matching references found._")
    }
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p7\P7.6A-QueueExecutor-Wiring-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath

if (-not (Test-Path -LiteralPath $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'

[void]$lines.Add('# P7.6A QueueExecutor Wiring Inventory')
[void]$lines.Add('')
[void]$lines.Add(('Generated: {0:yyyy-MM-dd HH:mm:ss zzz}' -f (Get-Date)))
[void]$lines.Add('')
[void]$lines.Add('This inventory is generated from the current repo tree to support repo-native P7 QueueExecutor SQL operational wiring.')
[void]$lines.Add('')
[void]$lines.Add('It intentionally reports existing files and likely wiring seams only. It does not mutate project files or runtime code.')

Add-FileListSection -Lines $lines -RootPath $root -Title 'QueueExecutor files' -RelativeFolder 'src\Workers\Migration.Workers.QueueExecutor' -Extensions @('.cs', '.csproj', '.json')
Add-FileListSection -Lines $lines -RootPath $root -Title 'GenericRuntime files' -RelativeFolder 'src\Core\Migration.GenericRuntime' -Extensions @('.cs', '.csproj', '.json')
Add-FileListSection -Lines $lines -RootPath $root -Title 'Orchestration files' -RelativeFolder 'src\Core\Migration.Orchestration' -Extensions @('.cs', '.csproj', '.json')
Add-FileListSection -Lines $lines -RootPath $root -Title 'Infrastructure.Sql files' -RelativeFolder 'src\Core\Migration.Infrastructure.Sql' -Extensions @('.cs', '.csproj', '.json')
Add-FileListSection -Lines $lines -RootPath $root -Title 'SQL operational worker host files' -RelativeFolder 'src\Hosts\Migration.Hosts.SqlOperationalWorker' -Extensions @('.cs', '.csproj', '.json')

Add-SearchHitsSection -Lines $lines -RootPath $root -Title 'Queue/worker related references in src' -RelativeFolder 'src' -Patterns @(
    'QueueExecutor',
    'IHostedService',
    'BackgroundService',
    'IServiceCollection',
    'AddHostedService',
    'ExecuteAsync',
    'Dequeue',
    'Enqueue',
    'Claim',
    'Lease',
    'Retry',
    'DeadLetter'
) -Extensions @('.cs', '.csproj', '.json')

Add-SearchHitsSection -Lines $lines -RootPath $root -Title 'Runtime/orchestration related references in src' -RelativeFolder 'src' -Patterns @(
    'GenericRuntime',
    'Orchestration',
    'MigrationRun',
    'Manifest',
    'DryRun',
    'Rollback',
    'Validation',
    'Connector'
) -Extensions @('.cs', '.csproj', '.json')

Add-SearchHitsSection -Lines $lines -RootPath $root -Title 'SQL persistence related references in src' -RelativeFolder 'src' -Patterns @(
    'SqlConnection',
    'DbConnection',
    'IDbConnection',
    'MigrationOperationalStore',
    'WorkItems',
    'ManifestRows',
    'Retry',
    'DeadLetter'
) -Extensions @('.cs', '.csproj', '.json')

[void]$lines.Add('')
[void]$lines.Add('## Notes for next P7 implementation set')
[void]$lines.Add('')
[void]$lines.Add('- Do not create top-level ``src\Migration.Infrastructure`` or ``src\Migration.Worker`` folders.')
[void]$lines.Add('- Do not introduce ``Migration.Infrastructure.Runtime`` unless that namespace already exists in repo-native code.')
[void]$lines.Add('- Prefer implementation wiring under existing ``src\Core\Migration.Infrastructure.Sql``, ``src\Core\Migration.Orchestration``, ``src\Core\Migration.GenericRuntime``, ``src\Workers\Migration.Workers.QueueExecutor``, and ``src\Hosts\Migration.Hosts.SqlOperationalWorker``.')
[void]$lines.Add('- Keep package versions centralized; do not add inline ``PackageReference Version`` attributes.')
[void]$lines.Add('- Keep validators PowerShell 5.1 compatible, StrictMode-safe, and defensive.')

Set-Content -Path $outputPath -Value $lines -Encoding UTF8

Write-Host "Wrote inventory: $outputPath"
