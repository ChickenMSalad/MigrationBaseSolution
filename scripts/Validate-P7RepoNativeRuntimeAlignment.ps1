Set-StrictMode -Version Latest
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
    return $normalized.Contains('\bin\') -or $normalized.Contains('\obj\')
}

function Assert-Exists {
    param(
        [string]$Root,
        [string]$RelativePath
    )

    $fullPath = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Required path missing: $RelativePath"
    }
}

function Assert-NotExists {
    param(
        [string]$Root,
        [string]$RelativePath
    )

    $fullPath = Join-Path $Root $RelativePath
    if (Test-Path -LiteralPath $fullPath) {
        throw "Invalid path still exists and should be removed: $RelativePath"
    }
}

function Assert-NoTextMatch {
    param(
        [string]$Root,
        [string]$Pattern,
        [string]$Description
    )

    $files = Get-ChildItem -Path $Root -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) } |
        Where-Object {
            $_.Extension -in @('.cs', '.csproj', '.props', '.targets', '.json', '.sln')
        }

    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($content -like "*$Pattern*") {
            throw "$Description found in $($file.FullName)"
        }
    }
}

function Assert-NoInlinePackageVersions {
    param([string]$Root)

    $projectFiles = Get-ChildItem -Path $Root -Filter '*.csproj' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) }

    foreach ($projectFile in $projectFiles) {
        [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw

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

$repositoryRoot = Get-RepositoryRoot
Write-Host "Repository root: $repositoryRoot"

Assert-Exists $repositoryRoot 'MigrationBaseSolution.sln'
Assert-Exists $repositoryRoot 'src\Core\Migration.Infrastructure\Migration.Infrastructure.csproj'
Assert-Exists $repositoryRoot 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
Assert-Exists $repositoryRoot 'src\Core\Migration.GenericRuntime\Migration.GenericRuntime.csproj'
Assert-Exists $repositoryRoot 'src\Core\Migration.Orchestration\Migration.Orchestration.csproj'
Assert-Exists $repositoryRoot 'src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj'
Assert-Exists $repositoryRoot 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj'
Assert-Exists $repositoryRoot 'database\sql\p7\001_operational_runtime_store.sql'
Assert-Exists $repositoryRoot 'database\sql\p7\002_operational_queue_procedures.sql'
Assert-Exists $repositoryRoot 'database\sql\p7\003_operational_queue_runtime_wiring.sql'

Assert-NotExists $repositoryRoot 'src\Migration.Infrastructure'
Assert-NotExists $repositoryRoot 'src\Migration.Worker'

Assert-NoTextMatch $repositoryRoot 'Migration.Infrastructure.Runtime' 'Invalid deleted runtime namespace reference'
Assert-NoTextMatch $repositoryRoot 'src\Migration.Worker' 'Invalid top-level worker path reference'
Assert-NoTextMatch $repositoryRoot 'src\Migration.Infrastructure\Runtime' 'Invalid top-level infrastructure runtime path reference'

Assert-NoInlinePackageVersions $repositoryRoot

Write-Host 'P7 repo-native runtime alignment validation passed.'
