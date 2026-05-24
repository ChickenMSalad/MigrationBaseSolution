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

function Assert-PathExists {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path missing: $RelativePath"
    }
}

function Assert-FileContains {
    param(
        [string]$RootPath,
        [string]$RelativePath,
        [string]$Text
    )

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file missing: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

function Assert-NoInlinePackageVersions {
    param([string]$RootPath)

    $projectFiles = Get-ChildItem -Path $RootPath -Filter '*.csproj' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) }

    foreach ($projectFile in $projectFiles) {
        [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw

        if ($null -eq $projectXml.Project -or
            $null -eq $projectXml.Project.PSObject.Properties['ItemGroup']) {
            continue
        }

        $itemGroups = @($projectXml.Project.ItemGroup)
        foreach ($itemGroup in $itemGroups) {
            if ($null -eq $itemGroup -or
                $null -eq $itemGroup.PSObject.Properties['PackageReference']) {
                continue
            }

            $packageReferences = @($itemGroup.PackageReference)
            foreach ($packageReference in $packageReferences) {
                if ($null -ne $packageReference -and
                    $null -ne $packageReference.PSObject.Properties['Version']) {
                    throw "Inline PackageReference Version found in $($projectFile.FullName)"
                }
            }
        }
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\ExecutionHistory\IOperationalExecutionHistoryWriter.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryOptions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryServiceCollectionExtensions.cs'
Assert-PathExists -RootPath $root -RelativePath 'docs\p7\P7.9B-SQL-Operational-Execution-History-Writer.md'

Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\ExecutionHistory\IOperationalExecutionHistoryWriter.cs' -Text 'IOperationalExecutionHistoryWriter'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs' -Text 'AttemptsTableName'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryServiceCollectionExtensions.cs' -Text 'AddSqlOperationalExecutionHistory'

Assert-NoInlinePackageVersions -RootPath $root

Write-Host 'P7.9B execution history writer package validation passed.'
