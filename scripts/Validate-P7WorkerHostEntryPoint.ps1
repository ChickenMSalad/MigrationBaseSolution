Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
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
    if ($normalized.Contains('\bin\')) { return $true }
    if ($normalized.Contains('\obj\')) { return $true }
    return $false
}

function Assert-FileExists {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $fullPath = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Required file missing: $RelativePath"
    }
}

function Get-XmlChildElements {
    param(
        [object]$Node,
        [string]$Name
    )

    if ($null -eq $Node) {
        return @()
    }

    if ($null -eq $Node.PSObject.Properties[$Name]) {
        return @()
    }

    return @($Node.$Name)
}

function Assert-NoInlinePackageVersions {
    param([string]$RootPath)

    $projectFiles = @(Get-ChildItem -Path $RootPath -Filter '*.csproj' -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) })

    foreach ($projectFile in $projectFiles) {
        [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
        $itemGroups = Get-XmlChildElements -Node $projectXml.Project -Name 'ItemGroup'

        foreach ($itemGroup in $itemGroups) {
            $packageReferences = Get-XmlChildElements -Node $itemGroup -Name 'PackageReference'

            foreach ($packageReference in $packageReferences) {
                if ($null -ne $packageReference -and $null -ne $packageReference.PSObject.Properties['Version']) {
                    throw "Inline PackageReference Version found in $($projectFile.FullName)"
                }
            }
        }
    }
}

function Assert-RequiredContent {
    param([string]$RootPath)

    $programPath = Join-Path $RootPath 'src\Migration.Worker\Program.cs'
    $programText = Get-Content -LiteralPath $programPath -Raw

    if ($programText -notmatch 'AddP7SqlOperationalRuntime') {
        throw 'Program.cs does not call AddP7SqlOperationalRuntime.'
    }

    if ($programText -notmatch 'SqlOperationalStartupProbeHostedService') {
        throw 'Program.cs does not register the startup readiness probe.'
    }

    $executorPath = Join-Path $RootPath 'src\Migration.Worker\SqlOperationalWorkerExecutor.cs'
    $executorText = Get-Content -LiteralPath $executorPath -Raw

    if ($executorText -notmatch 'p7-worker-host-smoke') {
        throw 'Worker executor smoke marker is missing.'
    }

    $settingsPath = Join-Path $RootPath 'src\Migration.Worker\appsettings.json'
    $settingsText = Get-Content -LiteralPath $settingsPath -Raw

    if ($settingsText -notmatch 'MigrationOperationalStore') {
        throw 'appsettings.json is missing the MigrationOperationalStore connection string key.'
    }

    if ($settingsText -notmatch 'SqlOperationalWorker') {
        throw 'appsettings.json is missing the SqlOperationalWorker section.'
    }
}

$repositoryRoot = Get-RepositoryRoot
Write-Host "Repository root: $repositoryRoot"

$requiredFiles = @(
    'src\Migration.Worker\Migration.Worker.csproj',
    'src\Migration.Worker\Program.cs',
    'src\Migration.Worker\SqlConnectionFactory.cs',
    'src\Migration.Worker\SqlOperationalWorkerExecutor.cs',
    'src\Migration.Worker\SqlOperationalStartupProbeHostedService.cs',
    'src\Migration.Worker\appsettings.json',
    'src\Migration.Worker\appsettings.Development.json',
    'scripts\Validate-P7WorkerHostEntryPoint.ps1',
    'docs\P7.5-WorkerHostEntryPoint-Notes.md',
    'README.md'
)

foreach ($relativePath in $requiredFiles) {
    Assert-FileExists -RootPath $repositoryRoot -RelativePath $relativePath
}

Assert-NoInlinePackageVersions -RootPath $repositoryRoot
Assert-RequiredContent -RootPath $repositoryRoot

Write-Host 'P7.5 worker host entry point validation passed.'
