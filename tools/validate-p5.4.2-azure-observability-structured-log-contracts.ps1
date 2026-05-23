Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing expected file: $Path"
    }
}

function Get-ProjectPackageReferences {
    param([Parameter(Mandatory=$true)][string]$ProjectPath)

    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) { return @() }

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    if ($null -eq $projectXml.Project) { return @() }

    $projectProperties = $projectXml.Project.PSObject.Properties
    if ($null -eq $projectProperties['ItemGroup']) { return @() }

    $references = New-Object System.Collections.Generic.List[object]
    foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
        if ($null -eq $itemGroup) { continue }
        $itemGroupProperties = $itemGroup.PSObject.Properties
        if ($null -eq $itemGroupProperties['PackageReference']) { continue }

        foreach ($packageReference in @($itemGroup.PackageReference)) {
            if ($null -ne $packageReference) { [void]$references.Add($packageReference) }
        }
    }

    return @($references.ToArray())
}

$expectedRelativeFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureStructuredLogSeverity.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureStructuredLogEvent.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureStructuredLogEventDescriptor.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/IAzureStructuredLogEventRegistry.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureStructuredLogEventRegistry.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Observability/AzureStructuredLogEventValidator.cs',
    'config/azure-runtime/observability/structured-log-events.sample.json'
)

foreach ($relativeFile in $expectedRelativeFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativeFile)
}

$badPackageReferences = New-Object System.Collections.Generic.List[string]
$projectFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

foreach ($project in @($projectFiles)) {
    foreach ($packageReference in @(Get-ProjectPackageReferences -ProjectPath $project.FullName)) {
        $properties = $packageReference.PSObject.Properties
        if ($null -ne $properties['Version']) {
            [void]$badPackageReferences.Add($project.FullName)
            break
        }
    }
}

if (@($badPackageReferences).Length -gt 0) {
    throw ("Inline PackageReference Version attributes detected:`n - " + (($badPackageReferences | Sort-Object -Unique) -join "`n - "))
}

$coreProject = Join-Path $repoRoot 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}

Write-Host 'P5.4.2 Azure observability structured log contract validation passed.'
