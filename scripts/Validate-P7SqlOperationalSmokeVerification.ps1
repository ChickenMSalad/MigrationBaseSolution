Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
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
    param([string]$RootPath, [string]$RelativePath)

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path missing: $RelativePath"
    }
}

function Assert-PathMissing {
    param([string]$RootPath, [string]$RelativePath)

    $path = Join-Path $RootPath $RelativePath
    if (Test-Path -LiteralPath $path) {
        throw "Invalid path should not exist: $RelativePath"
    }
}

function Assert-NoForbiddenNamespaceInSrc {
    param([string]$RootPath)

    $srcPath = Join-Path $RootPath 'src'
    if (-not (Test-Path -LiteralPath $srcPath)) {
        throw 'Required path missing: src'
    }

    $needle = 'Migration.Infrastructure.Runtime'
    $files = Get-ChildItem -Path $srcPath -File -Recurse |
        Where-Object {
            -not (Test-IsIgnoredPath $_.FullName) -and
            $_.Extension -in @('.cs', '.csproj', '.props', '.targets', '.json')
        }

    foreach ($file in $files) {
        $content = Get-Content -Path $file.FullName -Raw
        if ($null -ne $content -and $content.Contains($needle)) {
            throw "Invalid runtime namespace reference found in $($file.FullName)"
        }
    }
}

function Assert-NoInlinePackageVersions {
    param([string]$RootPath)

    $projectFiles = Get-ChildItem -Path $RootPath -Filter '*.csproj' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) }

    foreach ($projectFile in $projectFiles) {
        [xml]$projectXml = Get-Content -Path $projectFile.FullName -Raw

        if ($null -eq $projectXml.Project -or
            $null -eq $projectXml.Project.PSObject.Properties['ItemGroup']) {
            continue
        }

        foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
            if ($null -eq $itemGroup -or
                $null -eq $itemGroup.PSObject.Properties['PackageReference']) {
                continue
            }

            foreach ($packageReference in @($itemGroup.PackageReference)) {
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

Assert-PathMissing -RootPath $root -RelativePath 'src\Migration.Infrastructure'
Assert-PathMissing -RootPath $root -RelativePath 'src\Migration.Worker'

Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'

Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\001_operational_runtime_store.sql'
Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\002_operational_queue_procedures.sql'
Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\003_operational_queue_runtime_wiring.sql'
Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\004_sql_operational_smoke_diagnostics.sql'

Assert-NoForbiddenNamespaceInSrc -RootPath $root
Assert-NoInlinePackageVersions -RootPath $root

Write-Host 'P7.7E SQL operational smoke verification validation passed.'
